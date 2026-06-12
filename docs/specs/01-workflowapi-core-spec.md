# 01. WorkflowAPI Core Specification

Status: v1 tightened draft  
Audience: specification authors, .NET generator authors, UI authors, Temporal binding authors  
Scope: compact generic durable execution workflow API specification and static workflow display only

## 1. Purpose

WorkflowAPI is a compact specification for describing durable execution workflow APIs.

It is intended to sit beside OpenAPI and AsyncAPI:

```text
OpenAPI      -> HTTP request/response APIs
AsyncAPI     -> event/message APIs
WorkflowAPI  -> durable execution workflow APIs
```

WorkflowAPI v1 is intentionally narrow. It describes the workflow contract and an optional declared display topology. It does not model the external caller that starts a workflow, and it does not attempt to become BPMN-light.

The v1 implementation focus is:

- a generic WorkflowAPI document model;
- workflow identity and metadata;
- one durable workflow entry point per workflow;
- signals sent to running workflows;
- queries against workflow state;
- updates that interact with and may mutate workflow state;
- activities invoked by workflows;
- child workflow references;
- optional bridge references for durable cross-boundary operations;
- optional declared display topology;
- schema references for workflow payloads;
- runtime bindings, with Temporal as the first binding;
- .NET attributes and fluent definitions for generation;
- static workflow display/reference UI.

## 2. Non-goals

WorkflowAPI v1 does not model:

- the external caller that starts a workflow;
- HTTP endpoints that initiate workflows;
- message subscriptions that initiate workflows;
- schedulers, cron triggers, CRM actions, portal actions, command handlers, or other application-specific starters;
- BPMN-style process control flow;
- gateways, lanes, pools, joins, compensation flows, or executable process semantics;
- runtime histories;
- runtime metrics;
- observed topology;
- catalogue collation;
- source polling;
- workflow control-plane actions;
- workflow editing;
- code generation from diagrams.

Caller-facing APIs belong in OpenAPI, AsyncAPI, implementation documentation, or application-specific architecture documents. WorkflowAPI describes the workflow-hosted durable execution surface itself.

## 3. Design principles

### 3.1 Generic core, runtime-specific bindings

The core vocabulary must be generic. Temporal is the first binding and the first implementation target, not the definition of the standard.

### 3.2 The workflow entry point is not the caller

The `run` object describes the workflow's durable entry point. In Temporal .NET, this corresponds to the method marked with `[WorkflowRun]`, commonly named `RunAsync`.

It does not describe who starts the workflow or how the workflow is started from outside the worker. The caller may be an HTTP API, message handler, scheduler, CLI, another workflow, CRM system, broker portal, storefront event or another application-specific trigger. Those surfaces are outside WorkflowAPI v1.

### 3.3 Activities are called activities

The durable execution units invoked by a workflow are called `activities`. WorkflowAPI does not rename them to generic tasks or durable execution steps in v1.

### 3.4 Declared display topology, not executable process logic

WorkflowAPI topology is a declared display map. It is intended to help humans and tools understand the shape of a workflow. It is not an executable control-flow model and does not prove all possible runtime paths.

### 3.5 Display only in v1

The v1 UI is a static reference/display UI. It consumes a WorkflowAPI document and renders workflow details and the declared display topology. It does not connect to Temporal or any other runtime for metrics or control-plane actions.

## 4. Top-level document shape

```yaml
workflowApi: 1.0.0
id: commerce-order-workflows
info: {}
host: {}
bindings: {}
workflows: {}
activities: {}
bridges: {}
components: {}
extensions: {}
```

There is no top-level `implements` object in v1. Implemented capabilities are expressed through `workflows`, `activities` and `bridges`.

## 5. Workflow example

```yaml
workflowApi: 1.0.0
id: commerce-order-workflows
info:
  title: Commerce Order Workflow API
  version: 1.0.0
  summary: Durable workflow contracts for e-commerce order fulfilment.
host:
  name: commerce-order-worker
  kind: api-worker
  owner: Commerce Platform Team
  domain: Commerce
bindings:
  temporal:
    namespace: Commerce.OrderService
    taskQueues:
      - order-service
workflows:
  order-fulfilment:
    title: Order Fulfilment
    summary: Fulfils an e-commerce order by reserving inventory, taking payment, registering shipping and sending confirmation email.
    run:
      operationId: runOrderFulfilment
      summary: Durable workflow entry point.
      input:
        schema:
          $ref: '#/components/schemas/OrderFulfilmentRequest'
      output:
        schema:
          $ref: '#/components/schemas/OrderFulfilmentResult'
      bindings:
        temporal:
          workflowRunMethod: RunAsync
          workflowType: OrderFulfilmentWorkflow
    signals:
      payment-authorised:
        operationId: payment-authorised
        input:
          schema:
            $ref: '#/components/schemas/PaymentAuthorisedSignal'
        bindings:
          temporal:
            signalName: PaymentAuthorised
    queries:
      get-status:
        operationId: get-status
        output:
          schema:
            $ref: '#/components/schemas/OrderStatus'
        bindings:
          temporal:
            queryName: GetStatus
    updates:
      change-delivery-address:
        operationId: change-delivery-address
        input:
          schema:
            $ref: '#/components/schemas/ChangeDeliveryAddressRequest'
        output:
          schema:
            $ref: '#/components/schemas/OrderFulfilmentResult'
        bindings:
          temporal:
            updateName: ChangeDeliveryAddress
    topology:
      entry: validate-order
      steps:
        validate-order:
          kind: activity
          title: Validate order
          ref:
            activity: validate-order
        check-and-block-inventory:
          kind: activity
          title: Check and block inventory
          ref:
            activity: check-and-block-inventory
        take-payment:
          kind: activity
          title: Take payment
          ref:
            activity: take-payment
        register-shipping:
          kind: activity
          title: Register shipping
          ref:
            activity: register-shipping
        send-confirmation-email:
          kind: activity
          title: Send confirmation email
          ref:
            activity: send-confirmation-email
      edges:
        - from: validate-order
          to: check-and-block-inventory
          label: Order valid
        - from: check-and-block-inventory
          to: take-payment
          label: Inventory reserved
        - from: take-payment
          to: register-shipping
          label: Payment captured
        - from: register-shipping
          to: send-confirmation-email
          label: Shipment registered
activities:
  validate-order:
    title: Validate order
    bindings:
      temporal:
        activityType: ValidateOrderActivity
  check-and-block-inventory:
    title: Check and block inventory
    bindings:
      temporal:
        activityType: CheckAndBlockInventoryActivity
  take-payment:
    title: Take payment
    bindings:
      temporal:
        activityType: TakePaymentActivity
  register-shipping:
    title: Register shipping
    bindings:
      temporal:
        activityType: RegisterShippingActivity
  send-confirmation-email:
    title: Send confirmation email
    bindings:
      temporal:
        activityType: SendConfirmationEmailActivity
components:
  schemas:
    OrderFulfilmentRequest:
      type: object
    OrderFulfilmentResult:
      type: object
    PaymentAuthorisedSignal:
      type: object
    ChangeDeliveryAddressRequest:
      type: object
    OrderStatus:
      type: string
```

## 6. v1 validation rules

A v1 validator should check that required top-level fields exist, workflow keys are unique, each concrete workflow has exactly one `run` object, `run` is treated as the workflow entry point rather than caller metadata, topology edges reference known steps, activity references resolve, Temporal binding fields are structurally valid when present, and no obvious secrets are present.
