namespace WorkflowApi.AspNetCore.Serialization;

using System.Text.Json;
using System.Text.Json.Nodes;
using WorkflowApi.Abstractions;

/// <summary>
/// Static writer — all output is a pure function of the input document; no mutable state is needed.
/// </summary>
public static class WorkflowApiJsonWriter
{
    private static readonly JsonSerializerOptions s_options = new() { WriteIndented = true };

    /// <summary>Serializes a <see cref="WorkflowApiDocument"/> to an indented JSON string.</summary>
    public static string Write(WorkflowApiDocument document)
    {
        var root = BuildDocument(document);
        return root.ToJsonString(s_options);
    }

    private static JsonObject BuildDocument(WorkflowApiDocument document)
    {
        var root = new JsonObject();
        root["workflowApi"] = document.WorkflowApi;

        if (document.Info is not null)
            root["info"] = BuildInfo(document.Info);

        root["host"] = BuildHost(document.Host);

        var bindingsNode = BuildBindings(document.Bindings);
        if (bindingsNode is not null)
            root["bindings"] = bindingsNode;

        if (document.Workflows is { Count: > 0 })
            root["workflows"] = BuildWorkflowMap(document.Workflows);

        if (document.Activities is { Count: > 0 })
            root["activities"] = BuildActivityMap(document.Activities);

        if (document.Bridges is { Count: > 0 })
            root["bridges"] = BuildBridgeMap(document.Bridges);

        if (document.Diagnostics is { Count: > 0 })
            root["diagnostics"] = BuildDiagnosticList(document.Diagnostics);

        return root;
    }

    private static JsonObject BuildInfo(InfoDef info)
    {
        var node = new JsonObject();
        node["title"] = info.Title;
        node["version"] = info.Version;
        if (info.Summary is not null)
            node["summary"] = info.Summary;
        if (info.Description is not null)
            node["description"] = info.Description;
        return node;
    }

    private static JsonObject BuildHost(HostDef host)
    {
        var node = new JsonObject();
        node["id"] = host.Id;
        if (host.Name is not null)
            node["name"] = host.Name;
        return node;
    }

    private static JsonObject? BuildBindings(BindingsDef bindings)
    {
        if (bindings.Temporal is null)
            return null;

        var node = new JsonObject();
        node["temporal"] = BuildTemporalBinding(bindings.Temporal);
        return node;
    }

    private static JsonObject BuildTemporalBinding(TemporalBindingDef temporal)
    {
        var node = new JsonObject();
        node["namespace"] = temporal.Namespace;
        node["taskQueue"] = temporal.TaskQueue;
        if (temporal.WorkerHost is not null)
            node["workerHost"] = temporal.WorkerHost;
        if (temporal.Sdk is not null)
            node["sdk"] = temporal.Sdk;
        return node;
    }

    private static JsonObject BuildWorkflowMap(IReadOnlyDictionary<string, WorkflowDef> workflows)
    {
        var node = new JsonObject();
        foreach (var key in workflows.Keys.Order(StringComparer.Ordinal))
            node[key] = BuildWorkflowDef(workflows[key]);
        return node;
    }

    private static JsonObject BuildWorkflowDef(WorkflowDef workflow)
    {
        var node = new JsonObject();
        node["name"] = workflow.Name;
        if (workflow.DisplayName is not null)
            node["displayName"] = workflow.DisplayName;
        node["run"] = BuildOperationDef(workflow.Run);
        if (workflow.Signals is { Count: > 0 })
            node["signals"] = BuildOperationMap(workflow.Signals);
        if (workflow.Queries is { Count: > 0 })
            node["queries"] = BuildOperationMap(workflow.Queries);
        if (workflow.Updates is { Count: > 0 })
            node["updates"] = BuildOperationMap(workflow.Updates);
        if (workflow.Bindings is not null)
        {
            var bindingsNode = BuildBindings(workflow.Bindings);
            if (bindingsNode is not null)
                node["bindings"] = bindingsNode;
        }
        return node;
    }

    private static JsonObject BuildOperationMap(IReadOnlyDictionary<string, OperationDef> operations)
    {
        var node = new JsonObject();
        foreach (var key in operations.Keys.Order(StringComparer.Ordinal))
            node[key] = BuildOperationDef(operations[key]);
        return node;
    }

    private static JsonObject BuildOperationDef(OperationDef op)
    {
        var node = new JsonObject();
        node["operationId"] = op.OperationId;
        if (op.Summary is not null)
            node["summary"] = op.Summary;
        if (op.Input is not null)
            node["input"] = BuildSchemaRef(op.Input);
        if (op.Output is not null)
            node["output"] = BuildSchemaRef(op.Output);
        if (op.Bindings is not null)
        {
            var bindingsNode = BuildBindings(op.Bindings);
            if (bindingsNode is not null)
                node["bindings"] = bindingsNode;
        }
        return node;
    }

    private static JsonObject BuildSchemaRef(SchemaRefDef schemaRef)
    {
        var node = new JsonObject();
        node["$ref"] = schemaRef.Ref;
        return node;
    }

    private static JsonObject BuildActivityMap(IReadOnlyDictionary<string, ActivityDef> activities)
    {
        var node = new JsonObject();
        foreach (var key in activities.Keys.Order(StringComparer.Ordinal))
            node[key] = BuildActivityDef(activities[key]);
        return node;
    }

    private static JsonObject BuildActivityDef(ActivityDef activity)
    {
        var node = new JsonObject();
        if (activity.Title is not null)
            node["title"] = activity.Title;
        if (activity.Summary is not null)
            node["summary"] = activity.Summary;
        if (activity.Group is not null)
            node["group"] = activity.Group;
        if (activity.Visibility is not null)
            node["visibility"] = activity.Visibility;
        return node;
    }

    private static JsonObject BuildBridgeMap(IReadOnlyDictionary<string, BridgeDef> bridges)
    {
        var node = new JsonObject();
        foreach (var key in bridges.Keys.Order(StringComparer.Ordinal))
            node[key] = BuildBridgeDef(bridges[key]);
        return node;
    }

    private static JsonObject BuildBridgeDef(BridgeDef bridge)
    {
        var node = new JsonObject();
        if (bridge.DisplayName is not null)
            node["displayName"] = bridge.DisplayName;
        node["kind"] = bridge.Kind;
        if (bridge.Summary is not null)
            node["summary"] = bridge.Summary;
        if (bridge.Operations is { Count: > 0 })
        {
            var ops = new JsonArray();
            foreach (var op in bridge.Operations)
                ops.Add(op);
            node["operations"] = ops;
        }
        return node;
    }

    private static JsonArray BuildDiagnosticList(IReadOnlyList<Diagnostic> diagnostics)
    {
        var arr = new JsonArray();
        foreach (var d in diagnostics)
            arr.Add(BuildDiagnostic(d));
        return arr;
    }

    private static JsonObject BuildDiagnostic(Diagnostic diagnostic)
    {
        var node = new JsonObject();
        node["severity"] = diagnostic.Severity.ToString().ToLowerInvariant();
        node["code"] = diagnostic.Code;
        node["message"] = diagnostic.Message;
        if (diagnostic.Target is not null)
            node["target"] = diagnostic.Target;
        return node;
    }
}
