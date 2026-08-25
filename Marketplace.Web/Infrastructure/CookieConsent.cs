namespace Marketplace.Web.Infrastructure;

// The consent choice itself lives in an essential cookie (always allowed, since we need it
// to remember the choice at all). Every non-essential cookie the app sets — recently-viewed
// products, the per-order delivery-method note — checks IsAccepted first and simply skips
// itself if the visitor hasn't accepted, instead of writing anyway.
public static class CookieConsent
{
    public const string CookieName = "mp_cookie_consent";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";

    public static bool IsAccepted(IRequestCookieCollection cookies) =>
        cookies.TryGetValue(CookieName, out var value) && value == Accepted;
}
