# 07 — References

Accessed: 2026-06-10

This file lists sources used to ground the WorkflowAPI design. The design intentionally borrows patterns from OpenAPI, AsyncAPI, Microsoft’s current ASP.NET Core OpenAPI support, Scalar, Temporal .NET, Temporal Nexus, Scrutor, and EventCatalog visualisation.

---

## Microsoft ASP.NET Core OpenAPI / .NET 10

1. **Generate OpenAPI documents — ASP.NET Core**  
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0  
   Key design lesson: ASP.NET Core generates OpenAPI documents only; interactive UIs such as Scalar/Swagger UI are separate.

2. **Use the generated OpenAPI documents**  
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0  
   Key design lesson: document endpoint and UI consumption are separate; Scalar integrates by consuming the OpenAPI endpoint.

3. **Customize OpenAPI documents**  
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/customize-openapi?view=aspnetcore-10.0  
   Key design lesson: document, operation, and schema transformers provide a clean customization model.

4. **XML documentation comment support for OpenAPI**  
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/openapi-comments?view=aspnetcore-10.0  
   Key design lesson: XML docs should feed summaries/descriptions and reduce annotation noise.

5. **Overview of OpenAPI support in ASP.NET Core API apps**  
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0  
   Key design lesson: `AddOpenApi` registers generation services; `MapOpenApi` maps endpoints.

6. **Deprecation of WithOpenApi extension method**  
   https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/withopenapi-deprecated?view=aspnetcore-10.0  
   Key design lesson: avoid old Swashbuckle-era extension patterns; follow the newer document-generation pipeline.

7. **Microsoft.AspNetCore.OpenApi NuGet package**  
   https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/  
   Key design lesson: built-in OpenAPI generation is now the default Microsoft-supported direction for ASP.NET Core.

---

## Scalar

8. **Scalar ASP.NET Core integration**  
   https://scalar.com/products/api-references/integrations/aspnetcore/integration  
   Key design lesson: modern API reference UI should consume a spec endpoint and remain separate from generation.

---

## OpenAPI and AsyncAPI

9. **OpenAPI Specification 3.1.0**  
   https://swagger.io/specification/  
   Key design lesson: define a standard machine-readable interface that humans and computers can use to understand capabilities.

10. **OpenAPI Extensions**  
    https://swagger.io/docs/specification/v3_0/openapi-extensions/  
    Key design lesson: support `x-*` extensions for non-standard metadata.

11. **AsyncAPI latest specification**  
    https://www.asyncapi.com/docs/reference/specification/latest  
    Key design lesson: separate generic API contract from bindings.

12. **AsyncAPI bindings overview**  
    https://www.asyncapi.com/docs/reference/bindings  
    Key design lesson: protocol-specific details can be attached to servers, channels, operations, or messages.

13. **Adding bindings — AsyncAPI**  
    https://www.asyncapi.com/docs/concepts/asyncapi-document/adding-bindings  
    Key design lesson: bindings provide standard protocol-specific details without polluting the generic model.

14. **Extending the AsyncAPI specification**  
    https://www.asyncapi.com/docs/concepts/asyncapi-document/extending-specification  
    Key design lesson: support extensions for domain-specific information not covered by the core spec.

---

## Temporal .NET and Temporal Nexus

15. **Temporal .NET worker processes**  
    https://docs.temporal.io/develop/dotnet/workers/run-worker-process  
    Key design lesson: workers are created with client and worker options including task queue, workflows, and activities.

16. **Temporal .NET message passing**  
    https://docs.temporal.io/develop/dotnet/workflows/message-passing  
    Key design lesson: workflows can act like stateful services receiving Queries, Signals, and Updates.

17. **Temporal Workflow message passing encyclopedia**  
    https://docs.temporal.io/encyclopedia/workflow-message-passing  
    Key design lesson: Signals, Queries, and Updates are distinct message types and map naturally to WorkflowAPI operations.

18. **Temporal .NET SDK GitHub repository**  
    https://github.com/temporalio/sdk-dotnet  
    Key design lesson: .NET SDK uses attributes such as `[Workflow]`, `[WorkflowRun]`, `[WorkflowSignal]`, `[WorkflowQuery]`, `[WorkflowUpdate]`.

19. **Temporal .NET `WorkflowAttribute` API docs**  
    https://dotnet.temporal.io/api/Temporalio.Workflows.WorkflowAttribute.html  
    Key design lesson: the SDK provides technical runtime metadata, not full catalog/business metadata.

20. **Temporal .NET `TemporalWorkerOptions` API docs**  
    https://dotnet.temporal.io/api/Temporalio.Worker.TemporalWorkerOptions.html  
    Key design lesson: task queue and workflow/activity registration are worker options.

21. **Temporal Nexus overview**  
    https://docs.temporal.io/nexus  
    Key design lesson: Nexus endpoints route requests to a target namespace and task queue; callers do not need handler internals.

22. **Temporal .NET Nexus quickstart**  
    https://docs.temporal.io/develop/dotnet/nexus/quickstart  
    Key design lesson: Nexus endpoint names, services, operations, target namespaces, and target task queues are important catalog concepts.

23. **Temporal .NET Nexus feature guide**  
    https://docs.temporal.io/develop/dotnet/nexus/feature-guide  
    Key design lesson: Nexus service/operation contracts should be represented explicitly.

---

## Scrutor

24. **Scrutor GitHub repository**  
    https://github.com/khellang/Scrutor  
    Key design lesson: Scrutor provides assembly scanning and decoration extensions for `Microsoft.Extensions.DependencyInjection`.

25. **Scrutor NuGet package**  
    https://www.nuget.org/packages/Scrutor/  
    Key design lesson: use Scrutor internally for scanning workflow assemblies, while keeping the public WorkflowAPI API simple.

---

## EventCatalog and visualisation

26. **EventCatalog project**  
    https://github.com/event-catalog/eventcatalog  
    Key design lesson: central catalogs help teams document, visualize, govern, and understand ownership and connectivity.

27. **EventCatalog visualiser npm package**  
    https://www.npmjs.com/package/@eventcatalog/visualiser  
    Key design lesson: the visualiser package can be consumed independently as a React visualisation dependency.

28. **EventCatalog NodeGraph docs**  
    https://www.eventcatalog.dev/docs/development/components/components/nodegraph  
    Key design lesson: visualisation controls, React Flow/Mermaid modes, and graph exploration are useful patterns for WorkflowAPI Reference/Catalog.

29. **EventCatalog Flow docs**  
    https://eventcatalog.dev/docs/development/components/components/flow  
    Key design lesson: flow visualisations and layout persistence are relevant for workflow maps.

30. **React Flow**  
    https://reactflow.dev/  
    Key design lesson: if EventCatalog visualiser is too constrained, React Flow can be used directly behind the WorkflowAPI UI adapter.

---

## Design synthesis

The WorkflowAPI design borrows these specific patterns:

| Source | Pattern borrowed |
|---|---|
| Microsoft.AspNetCore.OpenApi | generation service + map endpoint + transformers + XML docs + no bundled UI |
| Scalar | separate modern reference UI consuming the generated document |
| OpenAPI | stable machine-readable contract; `info`; schemas; extensions |
| AsyncAPI | bindings for runtime/protocol-specific metadata |
| Temporal .NET | workflows, run, signals, queries, updates, activities, workers, task queues, namespaces |
| Temporal Nexus | cross-workflow/cross-namespace service and operation contracts |
| Scrutor | convention-based assembly scanning in .NET |
| EventCatalog visualiser | graph-oriented architecture/workflow visualisation patterns |


## Additional reference: HealthChecks.UI-style standalone polling configuration

- Xabaril `AspNetCore.Diagnostics.HealthChecks` sample `appsettings.json` demonstrates a standalone UI configured with a named endpoint list under `HealthChecksUI:HealthChecks`, with each endpoint containing `Name` and `Uri`, plus polling/notification timing values such as `EvaluationTimeinSeconds` and `MinimumSecondsBetweenFailureNotifications`.
  - Design lesson for WorkflowAPI: the standalone catalog should support a simple appsettings/env-var source list, periodic polling, in-memory status, source health, and partial-failure tolerance.


## GitHub Copilot and agentic repository tooling

- GitHub Copilot custom instructions support, including repository-wide `.github/copilot-instructions.md`, path-specific `.github/instructions/**/*.instructions.md`, and agent instruction support: https://docs.github.com/en/copilot/reference/custom-instructions-support
- GitHub Copilot agent skills, including project skills under `.github/skills`, `.claude/skills`, or `.agents/skills`: https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills
- GitHub Copilot prompt files and customization library: https://docs.github.com/en/copilot/tutorials/customization-library/prompt-files
