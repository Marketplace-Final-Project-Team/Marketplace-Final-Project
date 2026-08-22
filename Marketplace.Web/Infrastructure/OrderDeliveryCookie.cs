namespace Marketplace.Web.Infrastructure;

// The Order entity has no delivery-carrier column, so the choice made at checkout is
// remembered in a small per-order cookie instead of a database field — it's still real,
// server-set data (not a client-side mock), just not queryable/reportable like a DB column.
public static class OrderDeliveryCookie
{
    private const string Prefix = "mp_order_delivery_";
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(365);

    public static string NameFor(Guid orderId) => Prefix + orderId.ToString("N");
}
