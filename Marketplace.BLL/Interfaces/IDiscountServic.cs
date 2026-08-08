namespace Marketplace.BLL.Interfaces
{
    public interface IDiscountService
    {
        Task SetDiscountAsync(Guid productId, int percent, DateTime startDate, DateTime endDate);

        Task RemoveDiscountAsync(Guid productId);
    }
}