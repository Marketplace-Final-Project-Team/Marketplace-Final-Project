using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.DTOs
{
    public class LiqPayCallbackDto
    {
        public string order_id { get; set; }
        public string status { get; set; }
        public long payment_id { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string description { get; set; }
        public string paytype { get; set; }
        public string sender_card_mask2 { get; set; }
      
    }
}
