> **v1 tightened scope note**  
> This file is retained as a post-v1 design note unless a section explicitly says otherwise. WorkflowAPI v1 is limited to the compact durable execution workflow API specification, .NET generation from workflow attributes or fluent definitions, Temporal static binding metadata, and static workflow display. Runtime overlays, catalogue collation, source polling, workflow control-plane actions, BPMN-style modelling and runtime observability are outside v1 scope.

# 09 - WorkflowAPI Normative Document Model

Status: draft design supplement  
Audience: standards agents, schema agents, .NET generator agents, catalog/server agents, UI agents  
Scope: this document tightens the core WorkflowAPI DSL into a more complete normative document model. It exists because the earlier core spec describes the main concepts but does not yet define enough detail for an implementation team to build validators, generators, and catalog merge logic consistently.

## 1. Purpose

WorkflowAPI is a generic specification for durable workflow APIs. It describes workflow-hosting applications, their workflow capabilities, public interaction operations, topology, dependencies, cross-boundary bridges, payload schemas, runtime bindings, and optional observability metadata.

WorkflowAPI is intentionally analogous to OpenAPI and AsyncAPI, but it models durable, stateful, long-running processes rather than HTTP endpoints or message channels.

```text
OpenAPI      -> HTTP request/response APIs
AsyncAPI     -> event/message APIs
WorkflowAPI  -> durable workflow/process APIs
```

A WorkflowAPI document must be useful without a live workflow engine. Runtime systems such as Temporal provide execution and metrics overlays, but they are not the source of the generic DSL.

## 2. Design goals

WorkflowAPI must support:

1. Generic durable workflow contracts.
2. Workflow host descriptions.
3. Workflow run entry points.
4. Signals, queries, updates, and equivalent interaction operations.
5. Activities, steps, child workflows, and cross-boundary bridge operations.
6. Nested workflows and nested topology.
7. Cross-boundary workflow bridges, including Temporal Nexus as the first concrete binding.
8. Runtime bindings without making the core spec Temporal-specific.
9. JSON Schema-compatible payload definitions.
10. A central catalog that can merge many documents into a large graph.
11. A local per-service reference UI.
12. Runtime metrics overlays from workflow engines such as Temporal.
13. Stable identifiers suitable for versioning, diffing, and catalog collation.
14. Extension points comparable to OpenAPI `x-*` extensions and AsyncAPI bindings.

## 3. Non-goals

WorkflowAPI must not:

1. Replace workflow engine APIs.
2. Define workflow execution semantics completely.
3. Be Temporal-only.
4. Require a visual UI.
5. Require live access to a workflow engine.
6. Require a manually written DSL when a generator is available.
7. Describe arbitrary external clients as first-class sources in v1.
8. Contain credentials, tokens, certificates, or secrets.

## 4. Terminology

| Term | Definition |
|---|---|
| WorkflowAPI document | JSON or YAML document conforming to the WorkflowAPI specification. |
| Workflow host | An application, process, deployment, or module that implements workflow capabilities. In Temporal this is usually a worker process, but it may also be an API + worker combined application. |
| Catalog source | A URL, file, directory, artifact, or service endpoint from which a WorkflowAPI document is loaded. |
| Workflow | A durable, stateful, long-running process contract. |
| Workflow operation | An externally visible operation on a workflow, such as run/start, signal, query, update, cancel, terminate, or describe. |
| Step | A declared node in the workflow topology. A step corresponds to a durable execution primitive that has a natural attributable code artifact: `activity` (activity class), `childWorkflow` (workflow class invoked as child), or `bridge` (declared bridge operation). Topology must be explicitly declared via `[WorkflowApiStep]`/`[WorkflowApiEdge]` attributes or fluent definitions. |
| Nested workflow | A workflow started, invoked, embedded, or represented inside another workflow's topology. |
| Bridge | A cross-boundary durable operation connection. A bridge may cross namespace, task queue, application, cluster, workflow engine, trust, or ownership boundaries. Temporal Nexus is the first concrete bridge binding. |
| Binding | Runtime-specific metadata for a generic WorkflowAPI concept. |
| Topology | Declared graph of steps and edges inside a workflow. |
| Observed topology | Runtime-discovered paths from workflow histories or metrics. |
| Runtime overlay | Counts, durations, errors, retries, and status data joined onto declared WorkflowAPI entities. |

## 5. Document format

WorkflowAPI documents should be representable as JSON or YAML.

Recommended media types:

```text
application/vnd.workflowapi+json
application/vnd.workflowapi+yaml
```

Recommended endpoints:

```text
/.well-known/workflow-api.json
/workflow-api/{documentName}.json
```

The `.well-known` endpoint is the convention-first catalog discovery endpoint. The named endpoint pattern mirrors .NET OpenAPI's named document pattern and allows multiple views such as `v1`, `internal`, `runtime`, or `public`.

## 6. Top-level document shape

A WorkflowAPI document has the following top-level structure.

```yaml
workflowApi: 0.1.0
id: commerce-order-workflows
info: {}
host: {}
workflows: {}
bridges: {}
components: {}
bindings: {}
tags: []
externalDocs: []
extensions: {}
```

### 6.1 Top-level fields

| Field | Required | Description |
|---|---:|---|
| `workflowApi` | Yes | WorkflowAPI specification version. |
| `id` | Recommended | Stable document identifier. |
| `info` | Yes | Human and version metadata. |
| `host` | Recommended | Workflow-hosting application metadata. |
| `workflows` | Recommended | Map of workflow definitions. |
| `bridges` | Optional | Cross-boundary bridge definitions. |
| `components` | Optional | Shared schemas, examples, policies, owners, links, and reusable fragments. |
| `bindings` | Optional | Document-level runtime bindings. |
| `tags` | Optional | Tags used across the document. |
| `externalDocs` | Optional | Links to external documentation. |
| `extensions` | Optional | Free-form extension object. |

`workflows` is recommended but not strictly required because a document may publish only bridge definitions or shared components.

## 7. Identifiers and naming rules

WorkflowAPI must distinguish stable specification identifiers from runtime names.

Example:

```yaml
workflows:
  order-fulfilment:              # stable WorkflowAPI key
    title: Order Fulfilment
    bindings:
      temporal:
        workflowType: OrderFulfilmentWorkflow
```

Rules:

1. Map keys such as `order-fulfilment` are stable WorkflowAPI identifiers.
2. Runtime names such as `OrderFulfilmentWorkflow` belong in bindings.
3. Identifiers should use lowercase kebab-case where authored manually.
4. Generators may derive identifiers from runtime names but should normalize them deterministically.
5. Catalog merge keys must be based on stable IDs plus binding information, not display titles.

Recommended catalog keys:

```text
document:          document.id
host:              host.name + namespace if present
workflow:          namespace + workflow key OR document.id + workflow key
activity/task:     namespace + activity key OR document.id + activity key
bridge service:    bridge key + service name
bridge operation:  bridge key + service name + operation name
step:              workflow key + step key
```

`namespace` here is a generic computing concept for a runtime isolation boundary. Different runtimes may surface this under different names - Temporal calls it `namespace`, another runtime might call it `project`, `account`, or `tenant`. The binding section of the document maps it to the runtime-specific term.

## 8. `info` object

```yaml
info:
  title: Commerce Order Workflow API
  summary: Durable workflow contracts for the risk service.
  description: |
    Describes the workflows, activities, bridge operations, and runtime bindings
    implemented by the risk service workflow host.
  version: 1.0.0
  contact:
    name: Commerce Platform Team
    url: https://backstage.example/teams/ecr
  license:
    name: Internal
```

| Field | Required | Description |
|---|---:|---|
| `title` | Yes | Human-readable document title. |
| `summary` | Recommended | Short summary. |
| `description` | Optional | Longer markdown-capable description. |
| `version` | Yes | Document/spec version for the workflow host's contract. |
| `contact` | Optional | Contact/team details. |
| `license` | Optional | License or classification metadata. |

## 9. `host` object

`host` describes the workflow-hosting application or module that publishes the document.

```yaml
host:
  name: commerce-order-worker
  title: Commerce Order Worker
  kind: application
  owner: Commerce Platform Team
  domain: Risk
  lifecycle: production
  runtime: temporal
  environment: local
  tags:
    - B2B
    - Risk
```

| Field | Description |
|---|---|
| `name` | Stable host/application name. |
| `title` | Human-readable title. |
| `kind` | `application`, `worker`, `api-worker`, `module`, `service`, `library`, or custom value. |
| `owner` | Team or owning group. |
| `domain` | Business/domain grouping. |
| `lifecycle` | `experimental`, `development`, `preview`, `production`, `deprecated`, `retired`. |
| `runtime` | Primary workflow runtime, for example `temporal`. |
| `environment` | Optional environment label. |
| `tags` | Additional tags. |
| `bindings` | Host-level runtime bindings. |

Do not use producer/provider terminology in the core spec. The host implements workflow capabilities and may depend on other capabilities.

## 10. Runtime bindings

Bindings attach runtime-specific metadata to generic WorkflowAPI concepts.

A binding object is a map keyed by runtime or protocol name:

```yaml
bindings:
  temporal:
    namespace: Commerce.OrderService
    taskQueues:
      - order-service
```

Bindings may appear at:

1. Document level.
2. Host level.
3. Workflow level.
4. Operation level.
5. Step level.
6. Bridge level.
7. Bridge service/operation level.
8. Component level when useful.

Binding merge rule:

```text
more specific binding overrides or extends less specific binding
```

For example, a document-level Temporal namespace may be inherited by every workflow, while a workflow-level task queue may override a host-level default.

## 11. Workflows

The `workflows` object is a map of workflow identifiers to workflow definitions.

```yaml
workflows:
  order-fulfilment:
    title: Order Fulfilment
    summary: Fulfils an e-commerce order by reserving inventory, taking payment, registering shipping and sending confirmation email.
    version: 1.0.0
    owner: Commerce Platform Team
    domain: Risk
    lifecycle: production
    run: {}
    signals: {}
    queries: {}
    updates: {}
    operations: {}
    topology: {}
    dependsOn: {}
    bindings: {}
```

### 11.1 Workflow fields

| Field | Description |
|---|---|
| `title` | Human-readable workflow title. |
| `summary` | Short summary. |
| `description` | Longer markdown-capable description. |
| `version` | Workflow contract version. |
| `owner` | Owning team. |
| `domain` | Domain or bounded context. |
| `lifecycle` | Lifecycle status. |
| `visibility` | `public`, `internal`, `private`, or custom. |
| `run` | Primary run entry point. |
| `signals` | Map of asynchronous signal operations. |
| `queries` | Map of read-only query operations. |
| `updates` | Map of validated/update operations. |
| `operations` | Additional operation kinds such as cancel, terminate, reset, describe. |
| `topology` | Declared workflow graph. |
| `dependsOn` | Dependencies used by the workflow. |
| `emits` | Events/messages emitted by the workflow. |
| `consumes` | Events/messages consumed by the workflow, if relevant. |
| `policies` | Retry, timeout, SLA, retention, idempotency, and versioning policies. |
| `searchAttributes` | Declared business and runtime search dimensions. |
| `bindings` | Runtime-specific workflow bindings. |
| `extensions` | Extension object. |

## 12. Workflow operations

WorkflowAPI defines standard workflow interaction operation kinds.

| Kind | Description |
|---|---|
| `run` | Primary run entry point. |
| `signal` | Asynchronous message into a running workflow. |
| `query` | Read-only state query. |
| `update` | Validated state-changing interaction. |
| `cancel` | Request cancellation. |
| `terminate` | Force termination. |
| `describe` | Read metadata/state. |
| `reset` | Reset/replay operation. |
| `custom` | Runtime-specific or domain-specific operation. |

For how these kinds map to Temporal SDK concepts, see doc 03 (Temporal .NET Binding Design).

### 12.1 Operation object

```yaml
run:
  operationId: runOrderFulfilment
  title: Start risk enrichment
  summary: Starts enrichment for a company or lead.
  input:
    schema:
      $ref: '#/components/schemas/OrderFulfilmentRequest'
  output:
    schema:
      $ref: '#/components/schemas/OrderFulfilmentResult'
  examples:
    - $ref: '#/components/examples/StartRiskEnrichment'
  policies:
    idempotency:
      strategy: callerSuppliedId
  bindings:
    temporal:
      workflowType: OrderFulfilmentWorkflow
```

| Field | Description |
|---|---|
| `operationId` | Stable unique operation identifier within the document. |
| `title` | Human-readable title. |
| `summary` | Short summary. |
| `description` | Longer description. |
| `input` | Payload/schema input. |
| `output` | Payload/schema output. |
| `errors` | Declared error outcomes. |
| `examples` | Example references or inline examples. |
| `policies` | Operation-level policies. |
| `bindings` | Runtime-specific operation bindings. |
| `extensions` | Extension object. |

### 12.2 Signals

```yaml
signals:
  payment-authorised:
    operationId: creditConsentReceived
    title: Credit consent received
    input:
      schema:
        $ref: '#/components/schemas/PaymentAuthorisedSignal'
    bindings:
      temporal:
        signalName: PaymentAuthorised
```

Signals should be treated as externally meaningful contract operations, not merely implementation details.

### 12.3 Queries

```yaml
queries:
  get-status:
    operationId: getOrderWorkflowStatus
    title: Get status
    output:
      schema:
        $ref: '#/components/schemas/OrderWorkflowStatus'
    bindings:
      temporal:
        queryName: GetStatus
```

Queries should be side-effect-free from the perspective of the workflow contract.

### 12.4 Updates

```yaml
updates:
  reregister-shipping:
    operationId: recalculateRisk
    title: Change delivery address
    input:
      schema:
        $ref: '#/components/schemas/RecalculateOrderFulfilmentRequest'
    output:
      schema:
        $ref: '#/components/schemas/OrderFulfilmentResult'
    bindings:
      temporal:
        updateName: RecalculateRisk
```

Updates represent validated interactions that may mutate workflow state and return a result.

## 13. Topology

A workflow topology declares intended structure. Tools must distinguish three modes:

| Mode | Source | Description |
|---|---|---|
| **Declared topology** | WorkflowAPI document | Steps and edges as specified in the document. |
| **Observed topology** | Runtime execution histories | Paths actually taken at runtime, from event histories or traces. |
| **Operational overlay** | Runtime metrics provider | Counts, durations, failures, SLA breaches. |

Topology is always **explicitly declared** using WorkflowAPI's own `[WorkflowApiStep]`/`[WorkflowApiEdge]` attributes on the workflow class, or via fluent definitions. It cannot be read from the workflow's implementation code. Temporal SDK attributes provide the public operation surface; WorkflowAPI attributes declare the topology.

```yaml
topology:
  entry: receive-lead
  steps: {}
  edges: []
```

| Field | Description |
|---|---|
| `entry` | Optional starting step key. |
| `steps` | Map of step identifiers to step definitions. |
| `edges` | Directed relationships between steps. |
| `layout` | Optional visual layout hints. |

Catalogs should be able to show declared-only, observed-only, and overlay-decorated views.

## 14. Step model

```yaml
steps:
  take-payment:
    kind: activity
    title: Take payment
    summary: Calls payment provider and stores the enrichment result.
    group: External enrichment
    input:
      schema:
        $ref: '#/components/schemas/PaymentRequest'
    output:
      schema:
        $ref: '#/components/schemas/PaymentResult'
    policies:
      timeout:
        scheduleToClose: PT2M
    bindings:
      temporal:
        activityType: TakePaymentActivity
```

### 14.1 Standard step kinds

Only step kinds with a natural, attributable code artifact are included.

| Kind | Description | Code artifact |
|---|---|---|
| `activity` | Executes an activity worker unit. | Activity class decorated with `[WorkflowApiActivity]`. |
| `childWorkflow` | Starts another workflow as a child. Runs independently; can be awaited or detached. | Workflow class decorated with `[WorkflowApi]`. |
| `bridge` | Calls a cross-boundary bridge operation (e.g. Temporal Nexus). | Declared in the `bridges` map. |
| `custom` | Custom or runtime-specific step. Escape hatch for extension. | Developer-defined. |

`timer` (`Workflow.DelayAsync`) and `subflow` are not included - they have no natural code artifact to attach a WorkflowAPI attribute to. `signal`, `query`, and `update` are operation kinds (what callers do to the workflow), not step kinds.

### 14.2 Step fields

| Field | Description |
|---|---|
| `kind` | Step kind. |
| `title` | Human-readable title. |
| `summary` | Short summary. |
| `description` | Longer description. |
| `group` | Optional visual/logical grouping. |
| `input` | Input payload schema. |
| `output` | Output payload schema. |
| `errors` | Error outcomes. |
| `policies` | Retry, timeout, SLA, compensation, idempotency. |
| `ref` | Cross-entity reference. Required for `childWorkflow` (`ref.workflow`) and `bridge` (`ref.bridge`, `ref.service`, `ref.operation`). Optional for `activity` - if step key matches a key in the top-level `activities` map the tooling links them by convention. |
| `bindings` | Runtime-specific metadata. |
| `extensions` | Extension object. |

## 15. Nested workflows

Child workflows start another named workflow through the runtime. The child runs independently and can be awaited or allowed to run detached.

```yaml
steps:
  generate-document:
    kind: childWorkflow
    title: Generate payment provider PDF
    ref:
      workflow: generate-dnb-pdf
      document: document-service-workflows
    invocation:
      mode: startAndWait
      cancellation: propagate
      result: required
    bindings:
      temporal:
        childWorkflowType: GenerateDnbPdfWorkflow
```

Child workflow fields:

| Field | Description |
|---|---|
| `ref.workflow` | Referenced WorkflowAPI workflow key. |
| `ref.document` | Optional referenced document ID. |
| `invocation.mode` | `startAndWait`, `startAsync`, `fireAndForget`, `signalWithStart`, `continueAsNew`, `custom`. |
| `invocation.cancellation` | `propagate`, `abandon`, `tryCancel`, `waitCancellationCompleted`, `custom`. |
| `invocation.result` | `required`, `ignored`, `optional`. |
| `bindings` | Runtime-specific child workflow metadata. |

## 16. Edges

Edges describe intended topology relationships.

```yaml
edges:
  - id: identify-to-enrich
    from: check-and-block-inventory
    to: take-payment
    relation: next
    condition: Inventory blocked
```

### 16.1 Edge fields

| Field | Required | Description |
|---|---:|---|
| `id` | Recommended | Stable edge identifier. |
| `from` | Yes | Source step key. |
| `to` | Yes | Target step key. |
| `relation` | Optional | Relationship kind. |
| `condition` | Optional | Human-readable branch condition. |
| `label` | Optional | Visual label. |
| `probability` | Optional | Expected or observed probability, if known. |
| `bindings` | Optional | Runtime-specific edge metadata. |
| `extensions` | Optional | Extension object. |

### 16.2 Standard edge relations

| Relation | Meaning |
|---|---|
| `next` | Normal sequential transition. |
| `conditional` | Conditional branch. |
| `parallel` | Parallel branch. |
| `join` | Join after parallel work. |
| `error` | Error/failure path. |
| `timeout` | Timeout path. |
| `retry` | Retry relation. |
| `compensates` | Compensation relation. |
| `childWorkflow` | Parent-to-child workflow relation. |
| `bridge` | Step-to-bridge relation. |
| `emits` | Emits event/message. |
| `consumes` | Consumes event/message. |
| `custom` | Custom relation. |

## 17. Dependencies

Dependencies describe capabilities or systems used by a workflow.

```yaml
dependsOn:
  workflows:
    - workflow: generate-dnb-pdf
      relation: childWorkflow
  bridges:
    - bridge: document-generation
      service: DocumentService
      operation: GeneratePdf
  externalSystems:
    - id: dnb
      name: payment provider
      kind: external-api
  events:
    - channel: risk.company.enriched.v1
      direction: publishes
```

Dependencies are used by catalog merge logic to connect multiple WorkflowAPI documents into a larger graph.

## 18. Bridges

A bridge is a generic cross-boundary durable operation surface. It exists because real workflow systems often need to cross boundaries:

1. Namespace boundaries.
2. Task queue boundaries.
3. Application ownership boundaries.
4. Cluster/account boundaries.
5. Trust/security boundaries.
6. Workflow-engine boundaries.
7. Network/protocol boundaries.

Temporal Nexus is the first concrete binding, but the core concept is generic.

### 18.1 Bridge definition

```yaml
bridges:
  risk-bridge:
    kind: durableOperationBridge
    title: Risk bridge
    summary: Exposes risk operations across workflow namespace boundaries.
    services:
      RiskService:
        title: Order Service
        operations:
          CalculateRisk:
            title: Register shipping
            input:
              schema:
                $ref: '#/components/schemas/CalculateOrderFulfilmentRequest'
            output:
              schema:
                $ref: '#/components/schemas/CalculateOrderFulfilmentResult'
            handledBy:
              workflow: order-fulfilment
    bindings:
      temporal:
        nexusEndpoint: commerce-prod
        targetNamespace: Commerce.OrderService
        targetTaskQueue: order-service
```

### 18.2 Bridge fields

| Field | Description |
|---|---|
| `kind` | Bridge kind, for example `durableOperationBridge`, `namespaceBridge`, `engineBridge`, `protocolBridge`. |
| `title` | Human-readable title. |
| `summary` | Short summary. |
| `description` | Longer description. |
| `services` | Map of service names to operations. |
| `source` | Optional source-side boundary metadata. |
| `target` | Optional target-side boundary metadata. |
| `security` | Security requirements or references. |
| `bindings` | Runtime-specific bridge bindings. |
| `extensions` | Extension object. |

### 18.3 Bridge operation

```yaml
operations:
  GeneratePdf:
    title: Generate PDF
    input:
      schema:
        $ref: '#/components/schemas/GeneratePdfRequest'
    output:
      schema:
        $ref: '#/components/schemas/GeneratePdfResult'
    handledBy:
      workflow: generate-document
    policies:
      timeout:
        response: PT30S
```

Bridge operations are externally meaningful durable operations. They are not merely topology steps.

### 18.4 Calling a bridge operation from a workflow step

```yaml
steps:
  register-shipping:
    kind: bridge
    title: Register shipping via Order Service
    ref:
      bridge: risk-bridge
      service: RiskService
      operation: CalculateRisk
    bindings:
      temporal:
        nexusEndpoint: commerce-prod
        nexusService: RiskService
        nexusOperation: CalculateRisk
```

### 18.5 Temporal Nexus binding

Temporal Nexus maps to the generic bridge model:

| WorkflowAPI bridge concept | Temporal Nexus concept |
|---|---|
| Bridge | Nexus Endpoint + routing boundary. |
| Bridge service | Nexus Service. |
| Bridge operation | Nexus Operation. |
| Target boundary | Target namespace and task queue. |
| Handler | Nexus operation handler, often workflow-backed. |
| Calling step | Nexus operation call from a workflow. |

Temporal-specific data must stay under the `temporal` binding.

```yaml
bindings:
  temporal:
    nexusEndpoint: commerce-prod
    nexusService: RiskService
    nexusOperation: CalculateRisk
    targetNamespace: Commerce.OrderService
    targetTaskQueue: order-service
```

## 18a. Activities

The optional top-level `activities` map declares reusable activity definitions that may be referenced from workflow topology steps via `ref.activity`.

Activities represent units of work executed by a worker - often side-effecting operations such as external API calls, data transformations, or database writes.

```yaml
activities:
  take-payment:
    title: Take payment
    summary: Calls payment provider and stores the enrichment result.
    group: External enrichment
    visibility: internal
    input:
      schema:
        $ref: '#/components/schemas/PaymentRequest'
    output:
      schema:
        $ref: '#/components/schemas/PaymentResult'
    expectedDuration: PT30S
    sla: PT2M
    criticality: high
    bindings:
      temporal:
        activityType: TakePaymentActivity
        taskQueue: order-service
```

### 18a.1 Activity fields

| Field | Description |
|---|---|
| `title` | Human-readable title. |
| `summary` | Short summary. |
| `description` | Longer markdown-capable description. |
| `group` | Visual/logical grouping label. |
| `visibility` | `public`, `internal`, or `hidden`. |
| `input` | Input payload schema. |
| `output` | Output payload schema. |
| `expectedDuration` | ISO 8601 duration for expected runtime. |
| `sla` | ISO 8601 duration SLA. |
| `criticality` | `critical`, `high`, `medium`, `low`. |
| `policies` | Retry, timeout, and SLA policies. |
| `bindings` | Runtime-specific activity binding (e.g. Temporal `activityType`). |
| `extensions` | Extension object. |

### 18a.2 Referencing activities from steps

```yaml
topology:
  steps:
    take-payment:
      kind: activity
      title: Take payment
      ref:
        activity: take-payment
```

If an activity is used in only one workflow and has no reuse value, its metadata may be declared inline on the step rather than in the top-level `activities` map.

## 19. Components

Components hold reusable definitions.

```yaml
components:
  schemas: {}
  examples: {}
  policies: {}
  securitySchemes: {}
  owners: {}
  externalSystems: {}
  links: {}
  tags: {}
```

### 19.1 Schemas

WorkflowAPI should use JSON Schema-compatible schema definitions.

```yaml
components:
  schemas:
    OrderFulfilmentRequest:
      type: object
      required:
        - orderId
      properties:
        orderId:
          type: string
        salesChannel:
          type: string
```

Schema references use JSON Pointer-style `$ref` values.

```yaml
input:
  schema:
    $ref: '#/components/schemas/OrderFulfilmentRequest'
```

### 19.2 Examples

```yaml
components:
  examples:
    StartRiskEnrichment:
      summary: Broker lead risk enrichment
      value:
        orderId: '315000000'
        salesChannel: Broker
```

### 19.3 Policies

Reusable policies avoid repeating common SLA/retry/timeout metadata.

```yaml
components:
  policies:
    defaultActivityRetry:
      retry:
        maxAttempts: 3
        initialInterval: PT1S
        backoffCoefficient: 2.0
    dnbSla:
      sla:
        warning: PT30S
        breach: PT2M
```

## 20. Policies

Policies may appear at document, host, workflow, operation, step, or bridge-operation level.

Standard policy groups:

| Policy | Description |
|---|---|
| `retry` | Retry expectations. |
| `timeout` | Timeout expectations. |
| `sla` | Business SLA/warning/breach thresholds. |
| `idempotency` | Idempotency strategy. |
| `retention` | Retention expectations. |
| `versioning` | Versioning/deprecation strategy. |
| `compensation` | Compensation behavior. |
| `security` | Security requirements. |
| `observability` | Metrics/logging/tracing hints. |

Example:

```yaml
policies:
  timeout:
    scheduleToClose: PT2M
  retry:
    maxAttempts: 3
    nonRetryableErrors:
      - ValidationException
  sla:
    warning: PT30S
    breach: PT2M
```

## 21. Search attributes and dimensions

WorkflowAPI should declare business search dimensions used for filtering and catalog overlays. In the generic spec, these are named and described without runtime-specific index types:

```yaml
searchAttributes:
  - name: BusinessProcess
    description: Business process name for catalog search.
  - name: SalesChannel
    description: Originating sales channel.
  - name: RiskClass
    description: Computed risk classification.
```

Runtime-specific index types (such as Temporal's `Keyword`, `Text`, `Int`, `DateTime`) belong in the runtime binding:

```yaml
bindings:
  temporal:
    searchAttributes:
      SalesChannel: Keyword
      RiskClass: Keyword
```

Do not place sensitive personal data in search dimensions unless the deployment and runtime explicitly support appropriate protection.

## 22. Events and AsyncAPI references

WorkflowAPI may reference event/message contracts but should not replace AsyncAPI.

```yaml
emits:
  events:
    - id: company-risk-enriched
      channel: risk.company.enriched.v1
      asyncApiRef: https://catalog.example/asyncapi/risk-events.json#/channels/risk.company.enriched.v1
```

```yaml
consumes:
  events:
    - id: payment-authorised
      channel: customer.credit-consent.received.v1
```

Use references to AsyncAPI documents for detailed event/message contracts where available.

## 23. Security

WorkflowAPI documents should support security metadata without including secrets.

```yaml
components:
  securitySchemes:
    oauth2:
      type: oauth2
      description: Entra ID OAuth2 access token.

workflows:
  order-fulfilment:
    security:
      - oauth2:
          scopes:
            - workflow.risk.start
```

Security requirements describe required authorization, not credential values.

## 24. Extensions

WorkflowAPI supports extension fields for experimental or vendor-specific metadata.

Recommended convention:

```yaml
x-company-costCenter: ECR-42
x-temporal-internal-note: Do not rely on this field in portable tooling.
```

Runtime bindings are preferred over `x-*` extensions when a structured binding exists.

## 25. Temporal binding overview

Temporal is the first implementation binding. Temporal-specific metadata belongs under `bindings.temporal`.

Common Temporal fields:

| Field | Applies to | Description |
|---|---|---|
| `targetHost` | document/host | Temporal frontend address; usually environment-specific and often omitted from portable specs. |
| `namespace` | document/host/workflow | Temporal namespace. |
| `taskQueue` | workflow/activity/host | Single task queue. |
| `taskQueues` | host/document | Multiple task queues. |
| `workflowType` | workflow/run | Temporal workflow type. |
| `activityType` | step/activity | Temporal activity type. |
| `signalName` | signal operation | Temporal signal name. |
| `queryName` | query operation | Temporal query name. |
| `updateName` | update operation | Temporal update name. |
| `childWorkflowType` | childWorkflow step | Temporal child workflow type. |
| `nexusEndpoint` | bridge/step | Temporal Nexus endpoint. |
| `nexusService` | bridge service/step | Temporal Nexus service. |
| `nexusOperation` | bridge operation/step | Temporal Nexus operation. |
| `targetNamespace` | bridge | Target namespace for Nexus routing. |
| `targetTaskQueue` | bridge | Target task queue for Nexus routing. |
| `searchAttributes` | workflow/document | Search attribute declarations. |

Example:

```yaml
bindings:
  temporal:
    namespace: Commerce.OrderService
    taskQueues:
      - order-service

workflows:
  order-fulfilment:
    bindings:
      temporal:
        workflowType: OrderFulfilmentWorkflow
        taskQueue: order-service
```

## 26. Catalog collation rules

A catalog server loads multiple WorkflowAPI documents and produces a merged graph.

### 26.1 Merge inputs

Catalog inputs may be:

1. URLs to `.well-known/workflow-api.json`.
2. Explicit spec endpoints.
3. Files.
4. Directories.
5. Build artifacts.
6. OCI/package artifacts.

### 26.2 Merge algorithm

1. Fetch and parse each document.
2. Validate document version.
3. Assign source identity.
4. Normalize keys.
5. Add hosts.
6. Add workflows.
7. Add operations.
8. Add topology steps.
9. Add bridge services and operations.
10. Resolve internal references.
11. Resolve cross-document references.
12. Build graph edges.
13. Record unresolved references as warnings.
14. Overlay runtime metrics when configured.

### 26.3 Overlay rules

Overlay is additive. It must not rewrite source documents.

Examples:

```text
Source document says: order-fulfilment has step take-payment.
Runtime overlay says: take-payment had 2,431 executions, 12 failures, p95 8.4s.
Catalog graph shows both.
```

### 26.4 Conflict handling

Conflicts should be warnings by default, errors only in strict validation mode.

Examples:

1. Two documents define the same workflow key with incompatible schemas.
2. A workflow step references a bridge operation no document implements.
3. Runtime binding claims a namespace/task queue not reachable by configured Temporal credentials.
4. A source fails to refresh but stale cache exists.

## 27. Runtime overlay model

Runtime overlays are not part of the core contract but should have a standard shape for UI and catalog interoperability.

```yaml
runtimeOverlay:
  timeRange:
    from: 2026-06-01T00:00:00Z
    to: 2026-06-10T00:00:00Z
  workflows:
    order-fulfilment:
      started: 2431
      completed: 2353
      failed: 12
      running: 37
      avgDurationMs: 1840
      p95DurationMs: 8400
  steps:
    order-fulfilment/take-payment:
      scheduled: 2398
      completed: 2353
      failed: 12
      retries: 73
      p95DurationMs: 8400
```

Runtime overlay queries should be time-range based and cacheable.

## 28. Validation levels

WorkflowAPI validators should support levels.

| Level | Description |
|---|---|
| `syntax` | JSON/YAML parses and required top-level fields exist. |
| `schema` | Document conforms to WorkflowAPI JSON Schema. |
| `semantic` | References, bindings, and identifiers are consistent. |
| `catalog` | Cross-document references can be resolved. |
| `runtime` | Runtime bindings can be checked against configured engine credentials. |

## 29. Minimal valid document

```yaml
workflowApi: 0.1.0
info:
  title: Order Workflows
  version: 1.0.0
workflows:
  order-fulfilment:
    title: Order Fulfilment
    run:
      input:
        schema:
          type: object
      output:
        schema:
          type: object
```

## 30. Full illustrative document

```yaml
workflowApi: 0.1.0
id: commerce-order-workflows
info:
  title: Commerce Order Workflow API
  summary: Durable workflow contracts for e-commerce order fulfilment.
  version: 1.0.0
host:
  name: commerce-order-worker
  kind: worker
  owner: Commerce Platform Team
  domain: Risk
  runtime: temporal
bindings:
  temporal:
    namespace: Commerce.OrderService
    taskQueues:
      - order-service
workflows:
  order-fulfilment:
    title: Order Fulfilment
    summary: Enriches a company and calculates risk.
    run:
      operationId: runOrderFulfilment
      input:
        schema:
          $ref: '#/components/schemas/OrderFulfilmentRequest'
      output:
        schema:
          $ref: '#/components/schemas/OrderFulfilmentResult'
      bindings:
        temporal:
          workflowType: OrderFulfilmentWorkflow
    signals:
      payment-authorised:
        title: Credit consent received
        input:
          schema:
            $ref: '#/components/schemas/PaymentAuthorisedSignal'
        bindings:
          temporal:
            signalName: PaymentAuthorised
    queries:
      get-status:
        title: Get status
        output:
          schema:
            $ref: '#/components/schemas/OrderWorkflowStatus'
        bindings:
          temporal:
            queryName: GetStatus
    updates:
      reregister-shipping:
        title: Change delivery address
        input:
          schema:
            $ref: '#/components/schemas/RecalculateOrderFulfilmentRequest'
        output:
          schema:
            $ref: '#/components/schemas/OrderFulfilmentResult'
        bindings:
          temporal:
            updateName: RecalculateRisk
    topology:
      entry: check-and-block-inventory
      steps:
        check-and-block-inventory:
          kind: activity
          title: Check and block inventory
          bindings:
            temporal:
              activityType: IdentifyCompanyActivity
        take-payment:
          kind: activity
          title: Take payment
          bindings:
            temporal:
              activityType: TakePaymentActivity
        generate-document:
          kind: childWorkflow
          title: Generate payment provider PDF
          ref:
            workflow: generate-dnb-pdf
          invocation:
            mode: startAndWait
            cancellation: propagate
          bindings:
            temporal:
              childWorkflowType: GenerateDnbPdfWorkflow
        register-shipping-via-bridge:
          kind: bridge
          title: Register shipping through Risk bridge
          ref:
            bridge: risk-bridge
            service: RiskService
            operation: CalculateRisk
      edges:
        - from: check-and-block-inventory
          to: take-payment
        - from: take-payment
          to: generate-document
        - from: take-payment
          to: register-shipping-via-bridge
bridges:
  risk-bridge:
    kind: durableOperationBridge
    title: Risk bridge
    services:
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
              workflow: order-fulfilment
    bindings:
      temporal:
        nexusEndpoint: commerce-prod
        targetNamespace: Commerce.OrderService
        targetTaskQueue: order-service
components:
  schemas:
    OrderFulfilmentRequest:
      type: object
      properties:
        orderId:
          type: string
    OrderFulfilmentResult:
      type: object
    PaymentAuthorisedSignal:
      type: object
    OrderWorkflowStatus:
      type: object
    RecalculateOrderFulfilmentRequest:
      type: object
    CalculateOrderFulfilmentRequest:
      type: object
    CalculateOrderFulfilmentResult:
      type: object
```

## 31. Implementation implications

Implementation agents should treat this document as the stronger contract model for validators and generators.

Required implementation artifacts:

1. WorkflowAPI object model matching this document.
2. JSON/YAML serializer.
3. JSON Schema validator for syntax/schema validation.
4. Semantic validator for references, bindings, and topology.
5. .NET metadata extractor.
6. Temporal binding extractor.
7. Catalog graph builder.
8. Runtime overlay model.
9. UI adapter model.

## 32. Open design questions

The following require later decisions:

1. Whether `workflowApi` version should use SemVer or fixed spec version identifiers.
2. Whether `components.schemas` should use pure JSON Schema 2020-12 or an OpenAPI-compatible subset.
3. Whether bridges should be top-level only or also directly defined under workflows.
4. Whether bridge services should be generic enough to cover HTTP/gRPC APIs or only durable workflow bridges.
5. How much observed topology should be standardized versus left to runtime overlays.
6. How formal the `security` model should be in v0.1.
7. Whether `dimensions` should become a first-class top-level section or stay workflow-local.

## 33. Rubber-duck design check

Questions implementation agents should ask while building:

1. Can this document be rendered without Temporal credentials?
2. Can a catalog merge this document with another service's document?
3. Can a Temporal runtime overlay join metrics to workflows and steps using stable keys?
4. Can a nested child workflow be represented both as a topology node and as a referenced workflow contract?
5. Can a Temporal Nexus operation be represented without putting Nexus-specific concepts in the generic core?
6. Can another runtime implement the same concepts through a different binding?
7. Can the same workflow contract be shown in local, test, and production with different runtime bindings?
8. Can the document avoid exposing secrets?
9. Can a minimal document still be useful?
10. Can advanced topology be added incrementally without breaking older tooling?
