namespace WorkflowApi.AspNetCore.Serialization;

using WorkflowApi.Abstractions;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Static writer — all output is a pure function of the input document; no mutable state is needed.
/// </summary>
public static class WorkflowApiYamlWriter
{
    /// <summary>Serializes a <see cref="WorkflowApiDocument"/> to a YAML string.</summary>
    public static string Write(WorkflowApiDocument document)
    {
        var root = BuildDocument(document);
        var yamlDoc = new YamlDocument(root);
        var stream = new YamlStream(yamlDoc);

        var sw = new StringWriter { NewLine = "\n" };
        stream.Save(sw, assignAnchors: false);

        // YamlStream wraps output in "---\n...\n" markers; strip them for clean output.
        var raw = sw.ToString();
        if (raw.StartsWith("---\n", StringComparison.Ordinal))
            raw = raw[4..];
        if (raw.EndsWith("\n...\n", StringComparison.Ordinal))
            raw = raw[..^4];
        return raw;
    }

    private static YamlMappingNode BuildDocument(WorkflowApiDocument document)
    {
        var root = new YamlMappingNode();
        root.Add("workflowApi", Scalar(document.WorkflowApi));

        if (document.Info is not null)
            root.Add("info", BuildInfo(document.Info));

        root.Add("host", BuildHost(document.Host));

        var bindingsNode = BuildBindings(document.Bindings);
        if (bindingsNode is not null)
            root.Add("bindings", bindingsNode);

        if (document.Workflows is { Count: > 0 })
            root.Add("workflows", BuildWorkflowMap(document.Workflows));

        if (document.Activities is { Count: > 0 })
            root.Add("activities", BuildActivityMap(document.Activities));

        if (document.Bridges is { Count: > 0 })
            root.Add("bridges", BuildBridgeMap(document.Bridges));

        if (document.Diagnostics is { Count: > 0 })
            root.Add("diagnostics", BuildDiagnosticList(document.Diagnostics));

        return root;
    }

    private static YamlMappingNode BuildInfo(InfoDef info)
    {
        var node = new YamlMappingNode();
        node.Add("title", Scalar(info.Title));
        node.Add("version", Scalar(info.Version));
        if (info.Summary is not null)
            node.Add("summary", Scalar(info.Summary));
        if (info.Description is not null)
            node.Add("description", Scalar(info.Description));
        return node;
    }

    private static YamlMappingNode BuildHost(HostDef host)
    {
        var node = new YamlMappingNode();
        node.Add("id", Scalar(host.Id));
        if (host.Name is not null)
            node.Add("name", Scalar(host.Name));
        return node;
    }

    private static YamlMappingNode? BuildBindings(BindingsDef bindings)
    {
        if (bindings.Temporal is null)
            return null;

        var node = new YamlMappingNode();
        node.Add("temporal", BuildTemporalBinding(bindings.Temporal));
        return node;
    }

    private static YamlMappingNode BuildTemporalBinding(TemporalBindingDef temporal)
    {
        var node = new YamlMappingNode();
        node.Add("namespace", Scalar(temporal.Namespace));
        node.Add("taskQueue", Scalar(temporal.TaskQueue));
        if (temporal.WorkerHost is not null)
            node.Add("workerHost", Scalar(temporal.WorkerHost));
        if (temporal.Sdk is not null)
            node.Add("sdk", Scalar(temporal.Sdk));
        return node;
    }

    private static YamlMappingNode BuildWorkflowMap(IReadOnlyDictionary<string, WorkflowDef> workflows)
    {
        var node = new YamlMappingNode();
        foreach (var key in workflows.Keys.Order(StringComparer.Ordinal))
            node.Add(key, BuildWorkflowDef(workflows[key]));
        return node;
    }

    private static YamlMappingNode BuildWorkflowDef(WorkflowDef workflow)
    {
        var node = new YamlMappingNode();
        node.Add("name", Scalar(workflow.Name));
        if (workflow.DisplayName is not null)
            node.Add("displayName", Scalar(workflow.DisplayName));
        if (workflow.Summary is not null)
            node.Add("summary", Scalar(workflow.Summary));
        node.Add("run", BuildOperationDef(workflow.Run));
        if (workflow.Signals is { Count: > 0 })
            node.Add("signals", BuildOperationMap(workflow.Signals));
        if (workflow.Queries is { Count: > 0 })
            node.Add("queries", BuildOperationMap(workflow.Queries));
        if (workflow.Updates is { Count: > 0 })
            node.Add("updates", BuildOperationMap(workflow.Updates));
        if (workflow.Topology is not null)
            node.Add("topology", BuildTopology(workflow.Topology));
        if (workflow.Bindings is not null)
        {
            var bindingsNode = BuildBindings(workflow.Bindings);
            if (bindingsNode is not null)
                node.Add("bindings", bindingsNode);
        }
        return node;
    }

    private static YamlMappingNode BuildTopology(TopologyDef topology)
    {
        var node = new YamlMappingNode();
        var nodesNode = new YamlMappingNode();
        // Preserve insertion order if it's a List/Dict literal; otherwise sort by ordinal key.
        foreach (var kv in topology.Nodes)
            nodesNode.Add(kv.Key, BuildTopologyNode(kv.Value));
        node.Add("nodes", nodesNode);

        var edgesSeq = new YamlSequenceNode();
        foreach (var e in topology.Edges)
        {
            var em = new YamlMappingNode();
            em.Add("from", Scalar(e.From));
            em.Add("to", Scalar(e.To));
            edgesSeq.Add(em);
        }
        node.Add("edges", edgesSeq);
        return node;
    }

    private static YamlMappingNode BuildTopologyNode(TopologyNodeDef n)
    {
        var node = new YamlMappingNode();
        node.Add("kind", Scalar(n.Kind));
        if (n.DisplayName is not null)
            node.Add("displayName", Scalar(n.DisplayName));
        if (n.Summary is not null)
            node.Add("summary", Scalar(n.Summary));
        if (n.ActivityRef is not null)
            node.Add("activityRef", Scalar(n.ActivityRef));
        if (n.WorkflowRef is not null)
            node.Add("workflowRef", Scalar(n.WorkflowRef));
        return node;
    }

    private static YamlMappingNode BuildOperationMap(IReadOnlyDictionary<string, OperationDef> operations)
    {
        var node = new YamlMappingNode();
        foreach (var key in operations.Keys.Order(StringComparer.Ordinal))
            node.Add(key, BuildOperationDef(operations[key]));
        return node;
    }

    private static YamlMappingNode BuildOperationDef(OperationDef op)
    {
        var node = new YamlMappingNode();
        node.Add("operationId", Scalar(op.OperationId));
        if (op.Summary is not null)
            node.Add("summary", Scalar(op.Summary));
        if (op.Input is not null)
            node.Add("input", BuildSchemaRef(op.Input));
        if (op.Output is not null)
            node.Add("output", BuildSchemaRef(op.Output));
        if (op.Bindings is not null)
        {
            var bindingsNode = BuildBindings(op.Bindings);
            if (bindingsNode is not null)
                node.Add("bindings", bindingsNode);
        }
        return node;
    }

    private static YamlMappingNode BuildSchemaRef(SchemaRefDef schemaRef)
    {
        var node = new YamlMappingNode();
        // $ref is the literal YAML key; SchemaRefDef.Ref values starting with '#' are auto-quoted.
        node.Add("$ref", Scalar(schemaRef.Ref));
        return node;
    }

    private static YamlMappingNode BuildActivityMap(IReadOnlyDictionary<string, ActivityDef> activities)
    {
        var node = new YamlMappingNode();
        foreach (var key in activities.Keys.Order(StringComparer.Ordinal))
            node.Add(key, BuildActivityDef(activities[key]));
        return node;
    }

    private static YamlMappingNode BuildActivityDef(ActivityDef activity)
    {
        var node = new YamlMappingNode();
        if (activity.Title is not null)
            node.Add("title", Scalar(activity.Title));
        if (activity.Summary is not null)
            node.Add("summary", Scalar(activity.Summary));
        if (activity.Group is not null)
            node.Add("group", Scalar(activity.Group));
        if (activity.Visibility is not null)
            node.Add("visibility", Scalar(activity.Visibility));
        return node;
    }

    private static YamlMappingNode BuildBridgeMap(IReadOnlyDictionary<string, BridgeDef> bridges)
    {
        var node = new YamlMappingNode();
        foreach (var key in bridges.Keys.Order(StringComparer.Ordinal))
            node.Add(key, BuildBridgeDef(bridges[key]));
        return node;
    }

    private static YamlMappingNode BuildBridgeDef(BridgeDef bridge)
    {
        var node = new YamlMappingNode();
        if (bridge.DisplayName is not null)
            node.Add("displayName", Scalar(bridge.DisplayName));
        node.Add("kind", Scalar(bridge.Kind));
        if (bridge.Summary is not null)
            node.Add("summary", Scalar(bridge.Summary));
        if (bridge.Operations is { Count: > 0 })
        {
            var ops = new YamlSequenceNode();
            foreach (var op in bridge.Operations)
                ops.Add(Scalar(op));
            node.Add("operations", ops);
        }
        return node;
    }

    private static YamlSequenceNode BuildDiagnosticList(IReadOnlyList<Diagnostic> diagnostics)
    {
        var seq = new YamlSequenceNode();
        foreach (var d in diagnostics)
            seq.Add(BuildDiagnostic(d));
        return seq;
    }

    private static YamlMappingNode BuildDiagnostic(Diagnostic diagnostic)
    {
        var node = new YamlMappingNode();
        node.Add("severity", Scalar(diagnostic.Severity.ToString().ToLowerInvariant()));
        node.Add("code", Scalar(diagnostic.Code));
        node.Add("message", Scalar(diagnostic.Message));
        if (diagnostic.Target is not null)
            node.Add("target", Scalar(diagnostic.Target));
        return node;
    }

    private static YamlScalarNode Scalar(string value) => new(value);
}
