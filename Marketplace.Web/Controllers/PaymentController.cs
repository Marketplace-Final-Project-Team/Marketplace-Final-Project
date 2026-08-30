using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.BLL.Services;
using Marketplace.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Marketplace.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;

        public PaymentController(IPaymentService paymentService, IOrderService orderService)
        {
            _paymentService = paymentService;
            _orderService = orderService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentRequest request)
        {
            var order = await _orderService.GetOrderByIdAsync(Guid.Parse(request.OrderId));

            if (order == null) {
                return NotFound("Order not found");
            }

            var paymentData = await _paymentService.CreatePaymentAsync(
                orderId: order.Id.ToString(),
                amount: order.TotalAmount,
                description: $"Оплата замовлення #{order.Id}",
                successUrl: request.SuccessUrl,
                cancelUrl: request.CancelUrl
            );

            return Ok(paymentData);
        }


        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromForm] string data, [FromForm] string signature)
        {
            

            var privateKey = await _paymentService.GetPrivateKeyAsync();
            var computedSignature = privateKey + data + privateKey;
            byte[] inputBytes = Encoding.UTF8.GetBytes(computedSignature);

            
            byte[] hashBytes = SHA1.HashData(inputBytes);
            string base64signature = Convert.ToBase64String(hashBytes);

            if (base64signature != signature)
            {
                return BadRequest("Invalid signature");
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(data));
            var callbackData = JsonSerializer.Deserialize<LiqPayCallbackDto>(json);
            if (callbackData == null)
                return BadRequest("Invalid data");

            if (callbackData?.status == "success")
            {
                var orderId = Guid.Parse(callbackData.order_id);
                await _orderService.ChangeOrderStatus(orderId, OrderStatus.Confirmed);
            }
            
            
            return Ok();








        }
    }


    

}
