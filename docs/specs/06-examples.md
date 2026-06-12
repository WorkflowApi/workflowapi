# 06. Examples

Status: v1 tightened examples

## 1. Example domain

The v1 examples use a generic e-commerce order fulfilment workflow.

The workflow demonstrates a compact durable execution flow:

1. validate the order;
2. check and block inventory;
3. take payment;
4. register shipping;
5. send confirmation email.

The example is intentionally generic and display-focused. It does not model the HTTP endpoint, storefront event, scheduler or other caller that starts the workflow.

## 2. Workflow class example

```csharp
using Temporalio.Workflows;
using WorkflowApi;

namespace Commerce.OrderService.Workflows;

[Workflow("OrderFulfilmentWorkflow")]
[WorkflowApi(
    Name = "order-fulfilment",
    Title = "Order Fulfilment",
    Summary = "Fulfils an e-commerce order by reserving inventory, taking payment, registering shipping and sending confirmation email.",
    Owner = "Commerce Platform Team",
    Domain = "Commerce")]
public sealed class OrderFulfilmentWorkflow
{
    [WorkflowRun]
    [WorkflowApiRun(
        OperationId = "runOrderFulfilment",
        Summary = "Durable workflow entry point for order fulfilment.")]
    public Task<OrderFulfilmentResult> RunAsync(OrderFulfilmentRequest request)
    {
        throw new NotImplementedException();
    }

    [WorkflowSignal("PaymentAuthorised")]
    [WorkflowApiSignal(OperationId = "payment-authorised")]
    public Task PaymentAuthorisedAsync(PaymentAuthorisedSignal signal)
    {
        return Task.CompletedTask;
    }

    [WorkflowQuery("GetStatus")]
    [WorkflowApiQuery(OperationId = "get-status")]
    public OrderStatus GetStatus() => OrderStatus.Open;

    [WorkflowUpdate("ChangeDeliveryAddress")]
    [WorkflowApiUpdate(OperationId = "change-delivery-address")]
    public Task<OrderFulfilmentResult> ChangeDeliveryAddressAsync(ChangeDeliveryAddressRequest request)
    {
        throw new NotImplementedException();
    }
}
```

`RunAsync` is modelled as the workflow entry point. The HTTP route, storefront event, message handler or scheduler that starts the workflow is not modelled in WorkflowAPI.

## 3. Activity examples

```csharp
[WorkflowApiActivity(Name = "check-and-block-inventory", Title = "Check and block inventory")]
public sealed class CheckAndBlockInventoryActivity { }

[WorkflowApiActivity(Name = "take-payment", Title = "Take payment")]
public sealed class TakePaymentActivity { }

[WorkflowApiActivity(Name = "register-shipping", Title = "Register shipping")]
public sealed class RegisterShippingActivity { }

[WorkflowApiActivity(Name = "send-confirmation-email", Title = "Send confirmation email")]
public sealed class SendConfirmationEmailActivity { }
```

The DSL calls these activities.

## 4. Fluent topology example

```csharp
public sealed class OrderFulfilmentWorkflowDefinition
    : WorkflowApiDefinition<OrderFulfilmentWorkflow>
{
    public override void Define(IWorkflowApiBuilder builder)
    {
        builder.Workflow("order-fulfilment")
            .Title("Order Fulfilment")
            .Run<OrderFulfilmentRequest, OrderFulfilmentResult>("runOrderFulfilment")
            .Signal<PaymentAuthorisedSignal>("payment-authorised")
            .Query<OrderStatus>("get-status")
            .Update<ChangeDeliveryAddressRequest, OrderFulfilmentResult>("change-delivery-address")
            .Topology(topology =>
            {
                topology.Step("validate-order", StepKind.Activity).Title("Validate order").Activity("validate-order");
                topology.Step("check-and-block-inventory", StepKind.Activity).Title("Check and block inventory").Activity("check-and-block-inventory");
                topology.Step("take-payment", StepKind.Activity).Title("Take payment").Activity("take-payment");
                topology.Step("register-shipping", StepKind.Activity).Title("Register shipping").Activity("register-shipping");
                topology.Step("send-confirmation-email", StepKind.Activity).Title("Send confirmation email").Activity("send-confirmation-email");

                topology.Edge("validate-order", "check-and-block-inventory").Label("Order valid");
                topology.Edge("check-and-block-inventory", "take-payment").Label("Inventory reserved");
                topology.Edge("take-payment", "register-shipping").Label("Payment captured");
                topology.Edge("register-shipping", "send-confirmation-email").Label("Shipment registered");
            });
    }
}
```
