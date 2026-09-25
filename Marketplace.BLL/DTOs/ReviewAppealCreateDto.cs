using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.DTOs
{
    public class ReviewAppealCreateDto
    {
        public Guid ReviewId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public float Rating { get; set; }
    }
}
