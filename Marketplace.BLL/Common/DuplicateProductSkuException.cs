namespace Marketplace.BLL.Common;

public sealed class DuplicateProductSkuException : InvalidOperationException
{
    public DuplicateProductSkuException(string sku)
        : base($"Артикул «{sku}» уже використовується іншим товаром цього магазину.")
    {
        Sku = sku;
    }

    public string Sku { get; }
}
