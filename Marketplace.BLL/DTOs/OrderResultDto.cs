using Marketplace.BLL.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.DTOs
{
    public class OrderResultDto : ServiceResult 
    {
        
        public string OrderId { get; set; }
        public PaymentDataDto PaymentData { get; set; }
    }
}
