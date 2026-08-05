using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.DTOs
{
    public class PaymentDataDto
    {
        public string Data { get; set; }
        public string Signature { get; set; }
        public string Url { get; set; }
    }
}
