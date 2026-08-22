namespace Marketplace.Web.Infrastructure;

// Same idea as OrderDeliveryCookie: the Order entity has no payment-method column, so the
// choice made at checkout (online vs. cash on delivery) is remembered per-order in a cookie
// instead. Real, server-set data — just not a queryable DB field.
public static class OrderPaymentCookie
{
    private const string Prefix = "mp_order_payment_";
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(365);

    public static string NameFor(Guid orderId) => Prefix + orderId.ToString("N");
}
