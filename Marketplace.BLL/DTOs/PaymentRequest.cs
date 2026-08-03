using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.DTOs
{
    public class PaymentRequest
    {
        public string OrderId { get; set; }
        public string SuccessUrl { get; set; }
        public string CancelUrl { get; set; }
        //public string? ServerUrl { get; set; }
    }
}
