namespace Marketplace.BLL.Interfaces
{
    public interface IChatToolDispatcher
    {
        Task<Object> ExecuteAsync(string functionName, Dictionary<string, object>? args);
    }
}
