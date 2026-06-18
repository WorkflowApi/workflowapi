using System.Reflection;
using WorkflowApi.Abstractions;
using WorkflowApi.Temporal.Documents;
using WorkflowApi.Temporal.Mapping;
using WorkflowApi.Temporal.Scanning;

namespace WorkflowApi.Temporal.Tests.Documents;

public sealed class DocumentFactoryTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static readonly MethodInfo _stubMethod =
        typeof(DocumentFactoryTests).GetMethod(nameof(StubMethod), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static void StubMethod() { }

    private static ScannedOperation Op(string id) => new(_stubMethod, id);

    private static ScannedWorkflow Workflow(
        Type type,
        string name,
        ScannedOperation? run = null,
        IReadOnlyList<ScannedOperation>? signals = null,
        IReadOnlyList<ScannedOperation>? queries = null,
        IReadOnlyList<ScannedOperation>? updates = null) =>
        new(type, name, run ?? Op("Run"),
            signals ?? [],
            queries ?? [],
            updates ?? []);

    private static ScannedActivity Activity(Type type, string id) =>
        new(type, _stubMethod, id);

    private static MappedWorker Worker(
        string taskQueue,
        string? ns = null,
        IReadOnlyList<ScannedWorkflow>? workflows = null,
        IReadOnlyList<ScannedActivity>? activities = null,
        IReadOnlySet<string>? registeredWorkflowTypeFullNames = null,
        IReadOnlySet<string>? registeredActivityTypeFullNames = null)
    {
        var wfs = workflows ?? [];
        var acts = activities ?? [];
        var registeredWfNames = registeredWorkflowTypeFullNames ?? wfs.Select(w => w.WorkflowType.FullName).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var registeredActNames = registeredActivityTypeFullNames ?? acts.Select(a => a.ActivityType.FullName).OfType<string>().ToHashSet(StringComparer.Ordinal);
        return new(taskQueue, ns, wfs, acts, registeredWfNames, registeredActNames);
    }

    private static WorkerMapping Mapping(
        IReadOnlyList<MappedWorker> workers,
        IReadOnlyList<Diagnostic>? diagnostics = null) =>
        new(workers, diagnostics ?? []);

    private static IReadOnlyList<WorkflowApiDocument> Build(WorkerMapping mapping) =>
        new DocumentFactory().Build(mapping);

    // Dummy types used as workflow/activity type tokens
    private sealed class WfTypeA { }
    private sealed class WfTypeB { }
    private sealed class ActTypeA { }
    private sealed class ActTypeB { }

    // ──────────────────────────────────────────────────────────────────────────
    // Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Empty_mapping_returns_empty_list()
    {
        var docs = Build(Mapping([]));
        Assert.Empty(docs);
    }

    [Fact]
    public void Single_worker_produces_one_document_with_temporal_binding()
    {
        var wf = Workflow(typeof(WfTypeA), "WfA");
        var act = Activity(typeof(ActTypeA), "act.do");
        var worker = Worker("q1", "ns", [wf], [act]);

        var docs = Build(Mapping([worker]));

        var doc = Assert.Single(docs);
        Assert.Equal("0.1.0", doc.WorkflowApi);
        Assert.Equal("q1", doc.Host.Id);
        Assert.Null(doc.Info);
        Assert.Equal("ns", doc.Bindings.Temporal!.Namespace);
        Assert.Equal("q1", doc.Bindings.Temporal.TaskQueue);
        Assert.Equal("temporal-dotnet", doc.Bindings.Temporal.Sdk);
    }

    [Fact]
    public void Workflow_with_run_signal_query_update_maps_all_operations()
    {
        var wf = Workflow(
            typeof(WfTypeA),
            "WfA",
            run: Op("Run"),
            signals: [Op("sig-a"), Op("sig-b")],
            queries: [Op("qry-a")],
            updates: [Op("upd-a")]);

        var docs = Build(Mapping([Worker("q1", workflows: [wf])]));

        var doc = Assert.Single(docs);
        var wfDef = doc.Workflows["WfA"];

        Assert.Equal("Run", wfDef.Run.OperationId);
        Assert.Equal(2, wfDef.Signals.Count);
        Assert.True(wfDef.Signals.ContainsKey("sig-a"));
        Assert.True(wfDef.Signals.ContainsKey("sig-b"));
        Assert.Single(wfDef.Queries);
        Assert.True(wfDef.Queries.ContainsKey("qry-a"));
        Assert.Single(wfDef.Updates);
        Assert.True(wfDef.Updates.ContainsKey("upd-a"));
    }

    [Fact]
    public void Workflow_with_null_run_throws()
    {
        var wf = new ScannedWorkflow(typeof(WfTypeA), "WfA", Run: null, [], [], []);
        var worker = Worker("q1", workflows: [wf]);

        var ex = Assert.Throws<InvalidOperationException>(() => Build(Mapping([worker])));
        Assert.Contains("WfA", ex.Message);
    }

    [Fact]
    public void Activities_null_when_worker_has_no_activities()
    {
        var workerNoActs = Worker("q1", workflows: [Workflow(typeof(WfTypeA), "WfA")]);
        var docsNoActs = Build(Mapping([workerNoActs]));
        Assert.Null(Assert.Single(docsNoActs).Activities);
    }

    [Fact]
    public void Activities_keyed_by_id_when_present()
    {
        var acts = new[]
        {
            Activity(typeof(ActTypeA), "a.x"),
            Activity(typeof(ActTypeB), "a.y")
        };
        var workerWithActs = Worker("q2", workflows: [Workflow(typeof(WfTypeB), "WfB")], activities: acts);
        var docsWith = Build(Mapping([workerWithActs]));
        var actDict = Assert.Single(docsWith).Activities!;
        Assert.Equal(2, actDict.Count);
        Assert.True(actDict.ContainsKey("a.x"));
        Assert.True(actDict.ContainsKey("a.y"));
    }

    [Fact]
    public void Two_workers_produce_two_documents_ordered_by_host_id()
    {
        // WorkerMapping guarantees workers ordered by TaskQueue ordinal; factory preserves order.
        var workerA = Worker("q-a", workflows: [Workflow(typeof(WfTypeA), "WfA")]);
        var workerB = Worker("q-b", workflows: [Workflow(typeof(WfTypeB), "WfB")]);

        var docs = Build(Mapping([workerA, workerB]));

        Assert.Equal(2, docs.Count);
        Assert.Equal("q-a", docs[0].Host.Id);
        Assert.Equal("q-b", docs[1].Host.Id);
    }

    [Fact]
    public void Null_namespace_falls_back_to_default()
    {
        var worker = Worker("q1", ns: null, workflows: [Workflow(typeof(WfTypeA), "WfA")]);
        var docs = Build(Mapping([worker]));

        var doc = Assert.Single(docs);
        Assert.Equal("default", doc.Bindings.Temporal!.Namespace);
    }

    [Fact]
    public void Throws_on_null_mapping()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentFactory().Build(null!));
    }

    [Fact]
    public void Diagnostics_partitioned_to_owning_worker()
    {
        // TRUE WF002 scenario: unscanned but registered with Worker A
        var wf = Workflow(typeof(WfTypeA), "WfA");

        // Worker A registers an unscanned type "Some.Unscanned.Workflow"
        var workerA = Worker(
            "q-a",
            workflows: [wf],
            registeredWorkflowTypeFullNames: new HashSet<string> { typeof(WfTypeA).FullName!, "Some.Unscanned.Workflow" });

        var workerB = Worker("q-b", workflows: [wf]);

        // WF002 diagnostic targeting the unscanned type (belongs to Worker A only)
        var diag = new Diagnostic(
            DiagnosticSeverity.Warning,
            "WF002",
            "Some message",
            "Some.Unscanned.Workflow");

        var docs = Build(Mapping([workerA, workerB], [diag]));

        Assert.Equal(2, docs.Count);
        var docA = docs.Single(d => d.Host.Id == "q-a");
        var docB = docs.Single(d => d.Host.Id == "q-b");

        Assert.Single(docA.Diagnostics);
        Assert.Equal("WF002", docA.Diagnostics[0].Code);
        Assert.Empty(docB.Diagnostics);
    }

    [Fact]
    public void WF001_orphan_diagnostics_do_not_appear_on_any_document()
    {
        // One worker with one scanned workflow
        var wf = Workflow(typeof(WfTypeA), "WfA");
        var worker = Worker("q1", workflows: [wf]);

        // WorkerMapping.Diagnostics includes WF001 for an orphan scanned type
        var orphanDiag = new Diagnostic(
            DiagnosticSeverity.Warning,
            "WF001",
            "Orphan message",
            typeof(WfTypeB).FullName);

        var docs = Build(Mapping([worker], [orphanDiag]));

        var doc = Assert.Single(docs);
        // Orphan diagnostics don't land on any document
        Assert.Empty(doc.Diagnostics);
    }
}
