namespace Marketplace.BLL.Common
{
    public class DataResult<T>: ServiceResult
    {
        public T? Data { get; set; }
    }
}
