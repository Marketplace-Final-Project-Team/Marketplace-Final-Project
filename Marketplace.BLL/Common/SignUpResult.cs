namespace Marketplace.BLL.Common
{
    public class SignUpResult: ServiceResult
    {
        public string? UserId { get; set; }
        public string? Token { get; set; }
    }
}
