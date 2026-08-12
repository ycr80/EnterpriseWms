using EnterpriseWms.Application;
using EnterpriseWms.Contracts;

namespace EnterpriseWms.Tests;

public sealed class OrderValidatorTests
{
    [Fact]
    public void RejectsEmptyOrder()
    {
        var exception = Assert.Throws<BusinessRuleException>(() => OrderValidator.Validate(new CreateStockOrderRequest { WarehouseId = 1 }));
        Assert.Equal("validation.failed", exception.Code);
    }

    [Fact]
    public void RejectsNonPositiveQuantity()
    {
        var request = new CreateStockOrderRequest { WarehouseId = 1, Items = new List<StockOrderItemRequest> { new() { ProductId = 1, Quantity = 0 } } };
        Assert.Throws<BusinessRuleException>(() => OrderValidator.Validate(request));
    }

    [Fact]
    public void RejectsDuplicateProductLines()
    {
        var request = new CreateStockOrderRequest
        {
            WarehouseId = 1,
            Items = new List<StockOrderItemRequest> { new() { ProductId = 1, Quantity = 1 }, new() { ProductId = 1, Quantity = 2 } }
        };
        Assert.Throws<BusinessRuleException>(() => OrderValidator.Validate(request));
    }

    [Fact]
    public void AcceptsValidMultiLineOrder()
    {
        var request = new CreateStockOrderRequest
        {
            WarehouseId = 1,
            Items = new List<StockOrderItemRequest> { new() { ProductId = 1, Quantity = 1 }, new() { ProductId = 2, Quantity = 2 } }
        };
        OrderValidator.Validate(request);
    }
}
