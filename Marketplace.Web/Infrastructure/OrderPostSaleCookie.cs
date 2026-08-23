namespace Marketplace.Web.Infrastructure;

// Same idea as OrderDeliveryCookie/OrderPaymentCookie: the Order entity has no
// return/dispute columns, so a submitted request is remembered per-order in a cookie
// instead of a database row. Real, server-set data — just not queryable/reportable
// by sellers/admins like a DB table would be.
public static class OrderPostSaleCookie
{
    private const string ReturnPrefix = "mp_order_return_";
    private const string DisputePrefix = "mp_order_dispute_";
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(365);

    public static string ReturnNameFor(Guid orderId) => ReturnPrefix + orderId.ToString("N");
    public static string DisputeNameFor(Guid orderId) => DisputePrefix + orderId.ToString("N");

    public static string Serialize(string reason, string? comment, DateTime submittedAtUtc) =>
        string.Join('|', Uri.EscapeDataString(reason), Uri.EscapeDataString(comment ?? string.Empty), submittedAtUtc.ToString("O"));

    public static PostSaleRequest? Parse(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var parts = rawValue.Split('|');
        if (parts.Length != 3 || !DateTime.TryParse(parts[2], null, System.Globalization.DateTimeStyles.RoundtripKind, out var submittedAt))
        {
            return null;
        }

        return new PostSaleRequest(Uri.UnescapeDataString(parts[0]), Uri.UnescapeDataString(parts[1]), submittedAt);
    }
}

public record PostSaleRequest(string Reason, string Comment, DateTime SubmittedAtUtc);
