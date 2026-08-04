namespace Marketplace.BLL.Common
{
    public class ServiceResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
    }
}
