# 06 — Examples

Version: **0.1 draft**  
Audience: implementation agents, documentation agents, sample app agents  
Status: proposal

---

## 1. Example project layout

```text
TemporalRiskService/
  TemporalRiskService.AppHost/
  TemporalRiskService.ApiWorker/
  TemporalRiskService.Workflows/
```

`TemporalRiskService.Workflows` contains:

```text
Contracts/
  RiskRequest.cs
  RiskResult.cs
  RiskStatus.cs
  CreditConsentSignal.cs

Workflows/
  RiskEnrichmentWorkflow.cs

Activities/
  IdentifyCompanyActivity.cs
  EnrichDunsDataActivity.cs
  CalculateRiskActivity.cs
```

Both API/worker host and callers can reference this assembly.

---

## 2. Workflow class example

```csharp
using Temporalio.Workflows;
using WorkflowApi;

namespace TemporalRiskService.Workflows;

/// <summary>
/// Enriches a company with D&B data and calculates the risk class.
/// </summary>
/// <remarks>
/// Used by broker, website, sales portal, and CRM lead routes.
/// </remarks>
[Workflow("RiskEnrichmentWorkflow")]
[WorkflowApi(
    Name = "risk-enrichment",
    Title = "B2B Risk Enrichment",
    Summary = "Enriches a company with D&B data and calculates risk.",
    Owner = "Team ECR",
    Domain = "Risk",
    Tags = new[] { "risk", "dnb", "b2b" })]
[WorkflowApiSearchAttribute("BusinessProcess", Type = "Keyword")]
[WorkflowApiSearchAttribute("Duns", Type = "Keyword")]
[WorkflowApiSearchAttribute("SalesChannel", Type = "Keyword")]
[WorkflowApiEdge("identify-company", "enrich-dnb")]
[WorkflowApiEdge("enrich-dnb", "calculate-risk")]
[WorkflowApiEdge("calculate-risk", "publish-event")]
public sealed class RiskEnrichmentWorkflow
{
    private RiskStatus _status = RiskStatus.Open;

    /// <summary>
    /// Starts risk enrichment for a company or lead.
    /// </summary>
    [WorkflowRun]
    [WorkflowApiRun(
        OperationId = "startRiskEnrichment",
        Summary = "Start risk enrichment")]
    public async Task<RiskResult> RunAsync(RiskRequest request)
    {
        _status = RiskStatus.IdentifyingCompany;

        var identity = await Workflow.ExecuteActivityAsync(
            (IdentifyCompanyActivity a) => a.ExecuteAsync(request),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromSeconds(30)
            });

        _status = RiskStatus.EnrichingDnb;

        var enrichment = await Workflow.ExecuteActivityAsync(
            (EnrichDunsDataActivity a) => a.ExecuteAsync(identity),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromMinutes(2)
            });

        _status = RiskStatus.CalculatingRisk;

        var result = await Workflow.ExecuteActivityAsync(
            (CalculateRiskActivity a) => a.ExecuteAsync(enrichment),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromSeconds(30)
            });

        _status = RiskStatus.Completed;
        return result;
    }

    /// <summary>
    /// Signals that the customer has granted credit-check consent.
    /// </summary>
    [WorkflowSignal("CreditConsentReceived")]
    [WorkflowApiSignal(
        Summary = "Credit consent received",
        Description = "Raised when the customer grants permission for credit checking.")]
    public Task CreditConsentReceivedAsync(CreditConsentSignal signal)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the current business status of the workflow.
    /// </summary>
    [WorkflowQuery("GetStatus")]
    [WorkflowApiQuery(Summary = "Get current status")]
    public RiskStatus GetStatus() => _status;

    /// <summary>
    /// Requests a recalculation using updated inputs.
    /// </summary>
    [WorkflowUpdate("RecalculateRisk")]
    [WorkflowApiUpdate(Summary = "Recalculate risk")]
    public Task<RiskResult> RecalculateRiskAsync(RecalculateRiskRequest request)
    {
        throw new NotImplementedException();
    }
}
```

---

## 3. Activity example

```csharp
using Temporalio.Activities;
using WorkflowApi;

namespace TemporalRiskService.Workflows;

[WorkflowApiActivity(
    Name = "enrich-dnb",
    Title = "Enrich D&B data",
    Summary = "Calls D&B and stores the enrichment result.",
    Group = "External enrichment",
    ExpectedDuration = "PT30S",
    Sla = "PT2M",
    Criticality = WorkflowApiCriticality.High)]
public sealed class EnrichDunsDataActivity
{
    [Activity("EnrichDunsDataActivity")]
    public Task<DnbEnrichmentResult> ExecuteAsync(CompanyIdentity identity)
    {
        throw new NotImplementedException();
    }
}
```

---

## 4. ASP.NET Core combined API + worker example

```csharp
using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using TemporalRiskService.Workflows;
using WorkflowApi;
using WorkflowApi.Temporal;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTemporalClient(options =>
{
    options.TargetHost = builder.Configuration["Temporal:TargetHost"] ?? "localhost:7233";
    options.Namespace = builder.Configuration["Temporal:Namespace"] ?? "default";
});

builder.Services.AddHostedTemporalWorker("risk-service")
    .AddWorkflow<RiskEnrichmentWorkflow>()
    .AddActivity<IdentifyCompanyActivity>()
    .AddActivity<EnrichDunsDataActivity>()
    .AddActivity<CalculateRiskActivity>();

builder.Services.AddWorkflowApi("v1")
    .ScanFromAssemblyOf<RiskEnrichmentWorkflow>()
    .WithTemporal(options =>
    {
        options.Namespace = builder.Configuration["Temporal:Namespace"] ?? "default";
        options.TaskQueue = "risk-service";
        options.WebUrl = builder.Configuration["Temporal:WebUrl"];
    });

var app = builder.Build();

app.MapPost("/risk/enrich", async (RiskRequest request, ITemporalClient client) =>
{
    var handle = await client.StartWorkflowAsync(
        (RiskEnrichmentWorkflow wf) => wf.RunAsync(request),
        new WorkflowOptions
        {
            Id = $"risk-{request.Duns}",
            TaskQueue = "risk-service"
        });

    return Results.Accepted($"/risk/enrich/{handle.Id}");
});

app.MapWorkflowApi();
app.MapWorkflowApiReference();

app.Run();
```

---

## 5. Generated WorkflowAPI YAML example

```yaml
workflowApi: 0.1.0
info:
  title: TemporalRiskService Workflow API
  version: 1.0.0
  summary: Durable workflow contracts for risk enrichment.

host:
  name: temporal-risk-service
  kind: api-worker
  owner: Team ECR
  domain: Risk
  tags:
    - risk
    - temporal

bindings:
  temporal:
    namespace: default
    taskQueues:
      - risk-service

implements:
  workflows:
    risk-enrichment:
      title: B2B Risk Enrichment
      summary: Enriches a company with D&B data and calculates risk.
      description: Used by broker, website, sales portal, and CRM lead routes.
      owner: Team ECR
      domain: Risk
      tags:
        - risk
        - dnb
        - b2b
      bindings:
        temporal:
          workflowType: RiskEnrichmentWorkflow
          taskQueue: risk-service
      run:
        operationId: startRiskEnrichment
        summary: Start risk enrichment
        input:
          schema:
            $ref: '#/components/schemas/RiskRequest'
        output:
          schema:
            $ref: '#/components/schemas/RiskResult'
      signals:
        creditConsentReceived:
          summary: Credit consent received
          description: Raised when the customer grants permission for credit checking.
          input:
            schema:
              $ref: '#/components/schemas/CreditConsentSignal'
          bindings:
            temporal:
              signalName: CreditConsentReceived
      queries:
        getStatus:
          summary: Get current status
          output:
            schema:
              $ref: '#/components/schemas/RiskStatus'
          bindings:
            temporal:
              queryName: GetStatus
      updates:
        recalculateRisk:
          summary: Recalculate risk
          input:
            schema:
              $ref: '#/components/schemas/RecalculateRiskRequest'
          output:
            schema:
              $ref: '#/components/schemas/RiskResult'
          bindings:
            temporal:
              updateName: RecalculateRisk
      steps:
        identify-company:
          kind: activity
          title: Identify company
          activityRef: '#/implements/activities/identify-company'
        enrich-dnb:
          kind: activity
          title: Enrich D&B data
          activityRef: '#/implements/activities/enrich-dnb'
        calculate-risk:
          kind: activity
          title: Calculate risk
          activityRef: '#/implements/activities/calculate-risk'
        publish-event:
          kind: event
          title: Publish company enriched event
      edges:
        - from: identify-company
          to: enrich-dnb
        - from: enrich-dnb
          to: calculate-risk
        - from: calculate-risk
          to: publish-event
      searchAttributes:
        - name: BusinessProcess
          type: Keyword
        - name: Duns
          type: Keyword
        - name: SalesChannel
          type: Keyword

  activities:
    enrich-dnb:
      title: Enrich D&B data
      summary: Calls D&B and stores the enrichment result.
      group: External enrichment
      expectedDuration: PT30S
      sla: PT2M
      criticality: high
      input:
        schema:
          $ref: '#/components/schemas/CompanyIdentity'
      output:
        schema:
          $ref: '#/components/schemas/DnbEnrichmentResult'
      bindings:
        temporal:
          activityType: EnrichDunsDataActivity
          taskQueue: risk-service

components:
  schemas:
    RiskRequest:
      type: object
      required:
        - duns
      properties:
        duns:
          type: string
        salesChannel:
          type: string
    RiskResult:
      type: object
      properties:
        riskClass:
          type: string
    RiskStatus:
      type: string
      enum:
        - Open
        - IdentifyingCompany
        - EnrichingDnb
        - CalculatingRisk
        - Completed
        - Failed
```

---

## 6. Aspire AppHost example

### 6.1 Per-service only

No catalog container:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var temporal = builder.AddTemporalServer("temporal");

builder.AddProject<Projects.TemporalRiskService_ApiWorker>("risk-service")
    .WithReference(temporal);

builder.Build().Run();
```

The service exposes `/workflow-api/reference` itself.

### 6.2 Multi-service catalog

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var temporal = builder.AddTemporalServer("temporal");

var riskService = builder.AddProject<Projects.TemporalRiskService_ApiWorker>("risk-service")
    .WithReference(temporal);

var offerService = builder.AddProject<Projects.OfferService_ApiWorker>("offer-service")
    .WithReference(temporal);

builder.AddWorkflowApiCatalog("workflow-catalog")
    .WithCatalogSource(riskService)
    .WithCatalogSource(offerService)
    .WithTemporal(temporal, namespaceName: "default")
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

By convention:

```text
WithCatalogSource(riskService)
  -> http://risk-service/.well-known/workflow-api.json
```

---

## 7. Catalog source configuration example

```json
{
  "sources": [
    {
      "name": "risk-service",
      "url": "http://risk-service/.well-known/workflow-api.json"
    },
    {
      "name": "offer-service",
      "url": "http://offer-service/.well-known/workflow-api.json"
    }
  ],
  "runtime": {
    "temporal": {
      "targetHost": "temporal:7233",
      "namespace": "default",
      "webUrl": "http://temporal-ui:8233"
    }
  }
}
```

---

## 8. CLI examples

Export:

```bash
dotnet workflowapi export \
  --assembly ./bin/Release/net10.0/TemporalRiskService.Workflows.dll \
  --document v1 \
  --output ./artifacts/workflow-api/v1.json
```

Validate:

```bash
dotnet workflowapi validate ./artifacts/workflow-api/v1.json
```

Diff:

```bash
dotnet workflowapi diff ./old/v1.json ./new/v1.json
```

Publish:

```bash
dotnet workflowapi publish ./artifacts/workflow-api/v1.json \
  --catalog https://workflow-catalog.company.internal
```

---

## 9. Example rubber-duck review

For the sample above:

- The workflow contract is generated from the workflows assembly.
- Temporal binding is supplied by host configuration.
- Activity metadata is present but can be hidden/collapsed in UI.
- The single-service UI works without a catalog container.
- Aspire catalog container is only added for multi-service collation.
- No producer/provider terminology appears in the document.
- The document can be generated at runtime or exported in CI.
- Runtime metrics can be joined later by `workflowType`, `activityType`, and stable step IDs.

---

## 9. Standalone catalog server configuration example

This mode is for a .NET app/container that does not host workflows itself. It fetches WorkflowAPI documents from configured sources and builds a merged graph.

```json
{
  "WorkflowApiCatalog": {
    "Title": "Local WorkflowAPI Catalog",
    "Sources": [
      {
        "Name": "risk-worker",
        "Uri": "http://risk-worker/.well-known/workflow-api.json"
      },
      {
        "Name": "offer-worker",
        "Uri": "http://offer-worker/.well-known/workflow-api.json"
      }
    ],
    "Polling": {
      "Enabled": true,
      "IntervalSeconds": 30,
      "UseConditionalRequests": true
    },
    "Cache": {
      "Mode": "Memory",
      "KeepLastSuccessfulDocument": true,
      "OverlayTtlSeconds": 60
    },
    "Temporal": {
      "Enabled": true,
      "Connections": [
        {
          "Name": "local",
          "TargetHost": "temporal:7233",
          "Namespace": "default",
          "WebUrl": "http://temporal-ui:8233"
        }
      ]
    }
  }
}
```

Aspire usage:

```csharp
var temporal = builder.AddTemporalServer("temporal");

builder.AddWorkflowApiCatalog("workflow-catalog")
    .WithTemporal(temporal, namespaceName: "default")
    .WithCatalogSource(riskWorker)
    .WithCatalogSource(offerWorker)
    .WithRuntimeOverlay();
```

