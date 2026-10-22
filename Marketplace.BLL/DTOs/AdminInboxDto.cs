namespace Marketplace.BLL.DTOs
{
    public class AdminInboxDto
    {
        public IReadOnlyList<PostSaleRequestDto> OpenReturnRequests { get; set; } = new List<PostSaleRequestDto>(); 
        public IReadOnlyList<SupportTicketDto> OpenTickets { get; set; } = new List<SupportTicketDto>();              
        public IReadOnlyList<BusinessAdminDto> AdultRequests { get; set; } = new List<BusinessAdminDto>();     
    }
}
