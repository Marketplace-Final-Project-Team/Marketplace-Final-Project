namespace Marketplace.BLL.DTOs.Businesses;

public class BusinessOrderSummaryDto
{
    public int TotalOrders { get; init; }
    public int AwaitingProcessingOrders { get; init; }
    public int SoldItems { get; init; }
    public decimal Revenue { get; init; }
}
