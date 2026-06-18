using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WorkflowApi.Abstractions;

namespace WorkflowApi.Temporal.Topology;

/// <summary>
/// Extracts a linear topology (start → calls → end) for a workflow's [WorkflowRun] method by
/// parsing the source file with Roslyn and walking invocation expressions for
/// Workflow.ExecuteActivityAsync / Workflow.ExecuteChildWorkflowAsync.
///
/// Heuristics (hackweek): linear chain in source-order; parallel calls (Task.WhenAll) are
/// flattened. Subflows, conditionals, loops are not modelled. Fallback returns null when
/// the source file cannot be located.
/// </summary>
public sealed class WorkflowTopologyExtractor
{
    private readonly Dictionary<string, ClassDeclarationSyntax> _classesByFullName = new(StringComparer.Ordinal);
    private bool _indexed;

    public TopologyDef? Extract(Type workflowType)
    {
        ArgumentNullException.ThrowIfNull(workflowType);
        EnsureIndexed(workflowType.Assembly);

        var fullName = workflowType.FullName;
        if (fullName is null || !_classesByFullName.TryGetValue(fullName, out var classDecl))
            return null;

        var runMethod = classDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.AttributeLists.SelectMany(a => a.Attributes)
                .Any(a => GetAttributeName(a) is "WorkflowRun"));
        if (runMethod is null || (runMethod.Body is null && runMethod.ExpressionBody is null))
            return null;

        var calls = new List<TopologyCall>();
        var visitor = new InvocationVisitor(calls);
        visitor.Visit(runMethod);

        if (calls.Count == 0)
            return null;

        return BuildTopology(calls);
    }

    private static TopologyDef BuildTopology(List<TopologyCall> calls)
    {
        var nodes = new Dictionary<string, TopologyNodeDef>(StringComparer.Ordinal)
        {
            ["start"] = new("start", DisplayName: "Start", Summary: null, ActivityRef: null, WorkflowRef: null),
        };
        var edges = new List<TopologyEdgeDef>();
        var usedIds = new HashSet<string>(StringComparer.Ordinal) { "start", "end" };
        var orderedIds = new List<string> { "start" };

        foreach (var call in calls)
        {
            var id = MakeUniqueId(call, usedIds);
            usedIds.Add(id);
            var displayName = HumaniseType(call.TargetTypeName);
            nodes[id] = call.Kind switch
            {
                "activity" => new TopologyNodeDef("activity", displayName, null, ActivityRef: call.TargetTypeName, WorkflowRef: null),
                "childWorkflow" => new TopologyNodeDef("childWorkflow", displayName, null, ActivityRef: null, WorkflowRef: call.TargetTypeName),
                _ => new TopologyNodeDef(call.Kind, displayName, null, null, null),
            };
            orderedIds.Add(id);
        }
        orderedIds.Add("end");
        nodes["end"] = new("end", "End", null, null, null);

        for (var i = 0; i < orderedIds.Count - 1; i++)
            edges.Add(new TopologyEdgeDef(orderedIds[i], orderedIds[i + 1]));

        return new TopologyDef(nodes, edges);
    }

    private static string MakeUniqueId(TopologyCall call, HashSet<string> used)
    {
        var baseId = Slugify(call.TargetTypeName);
        if (!used.Contains(baseId))
            return baseId;
        var i = 2;
        while (used.Contains($"{baseId}-{i}")) i++;
        return $"{baseId}-{i}";
    }

    private static string Slugify(string typeName)
    {
        var stripped = StripSuffix(typeName);
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < stripped.Length; i++)
        {
            var c = stripped[i];
            if (char.IsUpper(c) && i > 0)
                sb.Append('-');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.Length == 0 ? "step" : sb.ToString();
    }

    private static string HumaniseType(string typeName)
    {
        var stripped = StripSuffix(typeName);
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < stripped.Length; i++)
        {
            var c = stripped[i];
            if (char.IsUpper(c) && i > 0 && !char.IsUpper(stripped[i - 1]))
                sb.Append(' ');
            sb.Append(i == 0 ? c : char.ToLower(c));
        }
        return sb.ToString();
    }

    private static string StripSuffix(string typeName) =>
        typeName.EndsWith("Activity", StringComparison.Ordinal) ? typeName[..^"Activity".Length]
        : typeName.EndsWith("Workflow", StringComparison.Ordinal) ? typeName[..^"Workflow".Length]
        : typeName;

    private void EnsureIndexed(Assembly assembly)
    {
        if (_indexed) return;
        _indexed = true;

        var projectRoot = FindProjectRoot(assembly.Location);
        if (projectRoot is null) return;

        var csFiles = Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        foreach (var path in csFiles)
        {
            string source;
            try { source = File.ReadAllText(path); }
            catch { continue; }

            SyntaxTree tree;
            try { tree = CSharpSyntaxTree.ParseText(source, path: path); }
            catch { continue; }

            var root = tree.GetRoot();
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var fn = GetFullName(classDecl);
                if (fn is not null)
                    _classesByFullName[fn] = classDecl;
            }
        }
    }

    private static string? FindProjectRoot(string? assemblyLocation)
    {
        if (string.IsNullOrWhiteSpace(assemblyLocation)) return null;
        var dir = Path.GetDirectoryName(assemblyLocation);
        while (!string.IsNullOrEmpty(dir))
        {
            if (Directory.EnumerateFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly).Any())
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    private static string? GetFullName(ClassDeclarationSyntax classDecl)
    {
        var ns = classDecl.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault()?.Name.ToString();
        return ns is null ? classDecl.Identifier.Text : $"{ns}.{classDecl.Identifier.Text}";
    }

    private static string GetAttributeName(AttributeSyntax attr)
    {
        var name = attr.Name.ToString();
        var idx = name.LastIndexOf('.');
        if (idx >= 0) name = name[(idx + 1)..];
        return name.EndsWith("Attribute", StringComparison.Ordinal) ? name[..^"Attribute".Length] : name;
    }

    private sealed record TopologyCall(string Kind, string TargetTypeName);

    private sealed class InvocationVisitor : CSharpSyntaxWalker
    {
        private readonly List<TopologyCall> _calls;
        public InvocationVisitor(List<TopologyCall> calls) { _calls = calls; }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax mae)
            {
                var methodName = mae.Name.Identifier.Text;
                var kind = methodName switch
                {
                    "ExecuteActivityAsync" or "StartActivityAsync" => "activity",
                    "ExecuteChildWorkflowAsync" or "StartChildWorkflowAsync" => "childWorkflow",
                    _ => null,
                };
                if (kind is not null && node.ArgumentList.Arguments.Count > 0)
                {
                    var firstArg = node.ArgumentList.Arguments[0].Expression;
                    if (firstArg is ParenthesizedLambdaExpressionSyntax lambda
                        && lambda.ParameterList.Parameters.Count == 1
                        && lambda.ParameterList.Parameters[0].Type is { } typeSyntax)
                    {
                        var targetType = typeSyntax.ToString();
                        var bareName = targetType[(targetType.LastIndexOf('.') + 1)..];
                        _calls.Add(new TopologyCall(kind, bareName));
                    }
                }
            }
            base.VisitInvocationExpression(node);
        }
    }
}
