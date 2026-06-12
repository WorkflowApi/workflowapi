> **v1 tightened scope note**  
> This file is retained as a post-v1 design note unless a section explicitly says otherwise. WorkflowAPI v1 is limited to the compact durable execution workflow API specification, .NET generation from workflow attributes or fluent definitions, Temporal static binding metadata, and static workflow display. Runtime overlays, catalogue collation, source polling, workflow control-plane actions, BPMN-style modelling and runtime observability are outside v1 scope.

# 03 - Temporal .NET Binding Design

Version: **0.1 draft**  
Audience: Temporal .NET agents, runtime binding agents, catalog agents  
Status: proposal

---

## 1. Purpose

This document defines the first WorkflowAPI binding: **Temporal .NET**.

Temporal is the first implementation target because it has a durable workflow programming model, .NET SDK attributes, workers, task queues, namespaces, signals, queries, updates, activities, child workflows, and Nexus.

The binding must distinguish:

```text
WorkflowAPI core contract
  generic workflows, operations, steps, dependencies

Temporal binding
  workflowType, activityType, namespace, taskQueue, Nexus endpoint/service/operation

Temporal runtime overlay
  counts, failures, durations, retries, histories, visibility queries
```

---

## 2. What Temporal .NET already provides

Temporal .NET uses attributes and explicit worker registration.

Key concepts:

- `[Workflow]`
- `[WorkflowRun]`
- `[WorkflowSignal]`
- `[WorkflowQuery]`
- `[WorkflowUpdate]`
- `[Activity]`
- `TemporalClientConnectOptions.Namespace`
- `TemporalClientConnectOptions.TargetHost`
- `TemporalWorkerOptions.TaskQueue`
- `TemporalWorkerOptions.AddWorkflow<T>()`
- `TemporalWorkerOptions.AddActivity(...)`
- `AddHostedTemporalWorker(...)`

Temporal workers register concrete workflow/activity implementations against a task queue. Namespace is a client/connection setting. Task queue is a worker option. Workflow/activity type names come from SDK attributes or defaults.

Important: Temporal .NET does **not** provide a built-in `.FromAssembly(...)` scan-and-register workflow API. WorkflowAPI can add scanning for documentation/spec generation, but it must not imply that Temporal runtime registration works that way.

---

## 3. What can be inferred

### 3.1 From Temporal SDK attributes

From `[Workflow]`:

- workflow type name;
- dynamic workflow flag;
- some workflow versioning/failure behavior metadata depending on SDK version.

From `[WorkflowRun]`:

- run method;
- input parameter type(s);
- return/result type.

From `[WorkflowSignal]`:

- signal name;
- input type(s);
- optional Temporal SDK description where available.

From `[WorkflowQuery]`:

- query name;
- output type;
- optional Temporal SDK description where available.

From `[WorkflowUpdate]`:

- update name;
- input type(s);
- output type;
- optional Temporal SDK description where available.

From `[Activity]`:

- activity type name;
- input type(s);
- output type;
- dynamic activity flag.

### 3.2 From .NET method/type signatures

- DTO types;
- async return result types;
- nullable annotations;
- generic wrappers;
- parameters.

### 3.3 From worker registration, if accessible

- task queue;
- registered workflow types;
- registered activity types;
- worker identity/name where configured.

### 3.4 From hosting/configuration

- Temporal namespace;
- target host;
- local vs production connection;
- task queue defaults;
- environment name.

### 3.5 From WorkflowAPI metadata

- human names;
- business descriptions;
- owners;
- domains;
- SLAs;
- topology;
- dependencies;
- visibility/security metadata.

---

## 4. What cannot be reliably inferred

The binding must be honest about gaps.

Not reliably inferable from Temporal SDK attributes alone:

- production namespace;
- production task queue;
- complete workflow topology;
- all possible branches;
- activity call order;
- external HTTP/database/service dependencies inside activities;
- business labels;
- ownership;
- SLA;
- data classification;
- Nexus endpoint routing configured outside code.

WorkflowAPI must support explicit metadata and transformers for these.

---

## 5. Binding object shape

### 5.1 Host-level Temporal binding

```yaml
bindings:
  temporal:
    targetHost: temporal.company.internal:7233
    namespace: Commerce.OrderService
    taskQueues:
      - order-service
    webUrl: https://temporal.company.internal
    environment: local
```

Fields:

| Field | Description |
|---|---|
| `targetHost` | Temporal frontend gRPC address. Should be optional in exported docs. |
| `namespace` | Temporal namespace. |
| `taskQueues` | Task queues associated with this host. |
| `webUrl` | Temporal Web URL for deep links. |
| `environment` | `local`, `dev`, `test`, `prod`, etc. |

### 5.2 Workflow-level binding

```yaml
bindings:
  temporal:
    workflowType: OrderFulfilmentWorkflow
    taskQueue: order-service
```

### 5.3 Activity-level binding

```yaml
bindings:
  temporal:
    activityType: TakePaymentActivity
    taskQueue: order-service
```

### 5.4 Signal/query/update binding

```yaml
signals:
  creditConsentReceived:
    bindings:
      temporal:
        signalName: PaymentAuthorised

queries:
  getStatus:
    bindings:
      temporal:
        queryName: GetStatus

updates:
  recalculateRisk:
    bindings:
      temporal:
        updateName: RecalculateRisk
```

### 5.5 Nexus binding

```yaml
implements:
  nexusServices:
    RiskService:
      operations:
        CalculateRisk:
          bindings:
            temporal:
              endpointName: order-service
              serviceName: RiskService
              operationName: CalculateRisk
              targetNamespace: Commerce.OrderService
              targetTaskQueue: order-service
```

`targetNamespace` and `targetTaskQueue` may come from deployment/catalog data rather than the service code.

---

## 6. Recommended C# usage

### 6.1 Workflows assembly

User-preferred layout:

```text
Risk.Workflows
  Contracts/DTOs
  Workflow classes
  Activity classes/method declarations
  WorkflowAPI attributes
```

Both API and worker reference it:

```text
Risk.Api     -> Risk.Workflows
Risk.Worker  -> Risk.Workflows
```

This is valid and practical for Temporal .NET because strongly typed callers reference workflow classes.

### 6.2 Worker host

```csharp
builder.Services.AddHostedTemporalWorker("order-service")
    .AddWorkflow<OrderFulfilmentWorkflow>()
    .AddActivity<IdentifyCompanyActivity>()
    .AddActivity<TakePaymentActivity>();

builder.Services.AddWorkflowApi("v1")
    .ScanFromAssemblyOf<OrderFulfilmentWorkflow>()
    .WithTemporal(options =>
    {
        options.Namespace = builder.Configuration["Temporal:Namespace"] ?? "default";
        options.TaskQueue = "order-service";
    });
```

### 6.3 Combined API + worker host

A single ASP.NET Core application can be both API and workflow host.

```csharp
builder.Services.AddHostedTemporalWorker("order-service")
    .AddWorkflow<OrderFulfilmentWorkflow>()
    .AddActivity<TakePaymentActivity>();

builder.Services.AddWorkflowApi("v1")
    .ScanFromAssemblyOf<OrderFulfilmentWorkflow>()
    .WithTemporal(...);

app.MapPost("/risk/enrich", async (OrderFulfilmentRequest request, ITemporalClient client) =>
{
    var handle = await client.StartWorkflowAsync(
        (OrderFulfilmentWorkflow wf) => wf.RunAsync(request),
        new WorkflowOptions
        {
            Id = $"risk-{request.OrderId}",
            TaskQueue = "order-service"
        });

    return Results.Accepted($"/risk/enrich/{handle.Id}");
});

app.MapWorkflowApi();
app.MapWorkflowApiReference();
```

WorkflowAPI still describes workflow-hosting capability. The fact that the same app also has HTTP endpoints is not a problem.

---

## 7. Strongly typed callers and shared workflow assemblies

Temporal .NET's strongly typed client pattern uses workflow classes in expressions:

```csharp
await client.StartWorkflowAsync(
    (OrderFulfilmentWorkflow wf) => wf.RunAsync(request),
    new WorkflowOptions
    {
        Id = workflowId,
        TaskQueue = "order-service"
    });
```

Therefore the caller/API normally references the workflow definition class/assembly.

Do not over-model pure interface-only workflow contracts unless Temporal .NET supports the specific pattern in the target SDK version and it has been validated.

Recommended practical split:

```text
Risk.Workflows
  workflows + activities + DTO contracts

Risk.Api
  references Risk.Workflows

Risk.Worker
  references Risk.Workflows
```

Alternative split for larger teams:

```text
Risk.Contracts
  DTOs only

Risk.Workflows
  workflow classes + workflow metadata
  references Risk.Contracts

Risk.Activities
  activity implementations
  references Risk.Contracts

Risk.Worker
  registers workflows/activities

Risk.Api
  references Risk.Contracts + Risk.Workflows
```

WorkflowAPI must support both.

---

## 8. Topology declaration for Temporal

Temporal execution histories can show what happened, but not the complete intended workflow graph.

WorkflowAPI should provide three sources for topology:

1. declared metadata using attributes/fluent definitions;
2. inferred metadata from workflow/activity calls where feasible;
3. observed runtime paths from Temporal histories as an overlay.

### 8.1 Attribute-based simple topology

```csharp
[WorkflowApiEdge("check-and-block-inventory", "take-payment")]
[WorkflowApiEdge("check-and-block-inventory", "manual-review", Label = "Low confidence")]
[WorkflowApiEdge("manual-review", "take-payment")]
public sealed class OrderFulfilmentWorkflow
{
}
```

### 8.2 Fluent topology

Recommended for non-trivial workflows:

```csharp
public sealed class OrderFulfilmentWorkflowApiDefinition
    : WorkflowApiDefinition<OrderFulfilmentWorkflow>
{
    public override void Define(IWorkflowApiBuilder builder)
    {
        builder.Step("check-and-block-inventory")
            .Activity("IdentifyCompanyActivity")
            .Title("Check and block inventory");

        builder.Step("take-payment")
            .Activity("TakePaymentActivity")
            .Title("Take payment");

        builder.Edge("check-and-block-inventory", "take-payment");
    }
}
```

### 8.3 Runtime-observed topology

Optional catalog overlay:

- read Workflow Execution histories;
- collapse events into logical step instances;
- aggregate transitions;
- detect observed-but-not-declared and declared-but-not-observed paths.

Do not make runtime-observed topology the source of the core document.

---

## 9. Search Attributes

Temporal Search Attributes are powerful business dimensions but require care.

WorkflowAPI should document intended Search Attributes:

```csharp
[WorkflowApiSearchDimension("BusinessProcess", Type = "Keyword", Summary = "Business process key")]
[WorkflowApiSearchDimension("OrderId", Type = "Keyword", Summary = "payment provider DUNS number")]
[WorkflowApiSearchDimension("SalesChannel", Type = "Keyword")]
public sealed class OrderFulfilmentWorkflow
{
}
```

Generated:

```yaml
searchAttributes:
  - name: BusinessProcess
    type: Keyword
  - name: OrderId
    type: Keyword
  - name: SalesChannel
    type: Keyword
```

Analyzer should warn about PII and sensitive fields.

---

## 10. Nexus modeling

Temporal Nexus introduces cross-application durable operations.

WorkflowAPI should model both:

- Nexus services/operations implemented by a workflow host;
- Nexus operations called by workflow steps.

### 10.1 Implemented Nexus operation

```yaml
implements:
  nexusServices:
    RiskService:
      operations:
        CalculateRisk:
          input:
            schema:
              $ref: '#/components/schemas/CalculateOrderFulfilmentRequest'
          output:
            schema:
              $ref: '#/components/schemas/CalculateOrderFulfilmentResult'
          handledBy:
            workflowRef: '#/implements/workflows/order-fulfilment'
          bindings:
            temporal:
              serviceName: RiskService
              operationName: CalculateRisk
```

### 10.2 Called Nexus operation

```yaml
workflows:
  offer-calculation:
    steps:
      register-shipping:
        kind: nexusOperation
        title: Register shipping
        calls:
          nexusService: RiskService
          operation: CalculateRisk
```

The central catalog joins these.

### 10.3 Endpoint routing

Temporal Nexus endpoint routing may be configured outside the code. Treat endpoint target namespace/task queue as environment/deployment metadata unless reliably available from the app.

---

## 11. Runtime overlay design

Temporal runtime APIs can support overlays but not core spec generation.

Useful runtime sources:

- Visibility/ListWorkflowExecutions;
- CountWorkflowExecutions;
- Search Attributes;
- GetWorkflowExecutionHistory;
- metrics from Prometheus/DataDog/OpenTelemetry;
- Temporal Web deep links.

Capabilities:

- workflow counts by type/status/time;
- failure counts;
- duration percentiles;
- activity-level durations from parsed histories;
- retry/failure hotspots;
- declared vs observed topology comparison.

Limitations:

- no direct “give me the workflow graph” API;
- activity retry details require careful event-history interpretation;
- history parsing at scale requires indexing/caching;
- visibility counts may have implementation-specific caveats;
- runtime access may be restricted in enterprise environments.

---

## 12. Aspire integration

### 12.1 Per-service reference

No separate container required for one service.

```csharp
builder.Services.AddWorkflowApi("v1")
    .ScanFromAssemblyOf<OrderFulfilmentWorkflow>()
    .WithTemporal(...);

app.MapWorkflowApi();
app.MapWorkflowApiReference();
```

### 12.2 Multi-service catalog

Separate catalog container useful for collation:

```csharp
var temporal = builder.AddTemporalServer("temporal");

var riskWorker = builder.AddProject<Projects.Risk_Worker>("commerce-order-worker")
    .WithReference(temporal);

var offerWorker = builder.AddProject<Projects.Offer_Worker>("offer-worker")
    .WithReference(temporal);

builder.AddWorkflowApiCatalog("workflow-catalog")
    .WithCatalogSource(riskWorker)
    .WithCatalogSource(offerWorker);
```

By convention, `.WithCatalogSource(project)` reads:

```text
/.well-known/workflow-api.json
```

Override only when needed:

```csharp
.WithCatalogSource(riskWorker, "/internal/workflow-api.json")
```

---

## 13. Validation rules for Temporal binding

| Rule | Severity | Notes |
|---|---|---|
| Workflow has no Temporal workflow type | Error if Temporal binding enabled | Inferred from `[Workflow]` or configured. |
| Workflow has no run method | Error | Temporal requires one run method. |
| Duplicate workflow type | Error | Within document/binding scope. |
| Duplicate signal/query/update names | Error | Per workflow. |
| Missing namespace | Warning | May be intentionally environment-neutral. |
| Missing task queue | Warning | Runtime binding incomplete. |
| Step references unknown activity type | Warning/error | Depends on strict mode. |
| Activity has no `[Activity]` and no explicit binding | Warning | May be logical step only. |
| Nexus operation lacks service/operation name | Error | Invalid binding. |
| Search attribute has PII flag | Warning | Must be reviewed. |

---

## 14. Rubber-duck review

### What works

- Accepts Temporal .NET's actual concrete workflow registration model.
- Does not invent a fake interface-only contract model.
- Separates workflows assembly from worker host binding.
- Supports user-preferred workflows/activities/contracts assembly layout.
- Models API+worker combined processes.
- Handles multiple task queues/namespaces.
- Treats runtime history parsing as overlay, not core truth.

### Risks

1. **Duplicate work between Temporal registration and WorkflowAPI scanning**  
   Mitigate with validation and future hooks into worker registration builders.

2. **Task queue ambiguity**  
   Workflow start options may target a task queue different from worker defaults. Host binding should document what the host polls, not every possible caller choice.

3. **Nexus endpoint routing externality**  
   Catalog may need deployment metadata to resolve endpoint -> namespace/task queue.

4. **Activity implementation leakage into API assemblies**  
   If workflows/activities/contracts are in one assembly, API references activity implementation types too. This is a pragmatic trade-off; teams can split assemblies later.

5. **Topology accuracy**  
   Declared topology must be treated as a curated map, not a formal executable proof.

