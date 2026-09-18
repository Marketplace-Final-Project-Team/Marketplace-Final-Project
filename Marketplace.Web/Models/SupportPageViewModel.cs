using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Models
{
    public class SupportPageViewModel
    {
        public bool IsAuthenticated { get; set; }
        public SupportTicketDto? ActiveTicket { get; set; }
    }
}