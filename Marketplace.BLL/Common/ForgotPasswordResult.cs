namespace Marketplace.BLL.Common
{
    public class ForgotPasswordResult: ServiceResult
    {
        public string? UserId { get; set; }
        public string? Token { get; set; }
        public bool AccountExists { get; set; }
    }
}
