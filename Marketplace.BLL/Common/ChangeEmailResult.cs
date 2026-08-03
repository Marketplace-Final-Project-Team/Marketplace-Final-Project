namespace Marketplace.BLL.Common
{
    public class ChangeEmailResult: ServiceResult
    {
        public string? UserId { get; set; }
        public string? Token { get; set; }
        public string? NewEmail { get; set; }
    }
}
