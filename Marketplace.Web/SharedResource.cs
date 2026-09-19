namespace Marketplace.Web
{
    // Marker type only - its purpose is to give IStringLocalizer<SharedResource> a stable
    // name to bind to (Resources/SharedResource.*.resx), for strings reused across many
    // views (nav labels, common buttons, statuses) instead of duplicating them per-view.
    // Must live in the root namespace, NOT Marketplace.Web.Resources: ResourcesPath is
    // already "Resources", and putting the marker inside a namespace of the same name
    // makes the localizer look for Resources/Resources/SharedResource.*.resx instead.
    public class SharedResource
    {
    }
}
