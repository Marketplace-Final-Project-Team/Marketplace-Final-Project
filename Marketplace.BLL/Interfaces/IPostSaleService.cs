using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Marketplace.BLL.Interfaces
{
    public interface IPostSaleService
    {
        // Заявка на повернення по конкретній позиції замовлення (одна на позицію — не на все
        // замовлення, щоб у змішаному замовленні повернення одного товару не чіпало інших
        // продавців). Вимагає причину, опис (мінімум 10 символів) і хоча б одне фото (макс. 5).
        Task<ServiceResult> CreateRequestAsync(string buyerId, Guid orderItemId, string reason, string comment, List<IFormFile> photos);

        // Заявки покупця по конкретному замовленню (по всіх його позиціях) — для сторінки деталей замовлення.
        Task<List<PostSaleRequestDto>> GetRequestsForOrderAsync(Guid orderId);

        // Усі заявки по замовленнях, що містять товари цього магазину — для кабінету продавця.
        Task<List<PostSaleRequestDto>> GetRequestsForBusinessAsync(Guid businessId);

        // Підтвердити або відхилити заявку.
        Task<ServiceResult> ResolveRequestAsync(Guid requestId, Guid businessId, PostSaleRequestStatus decision, string? sellerResponse);
    }
}
