using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Marketplace.Web.TagHelpers;

[HtmlTargetElement("time", Attributes = "utc-value")]
public class LocalTimeTagHelper : TagHelper
{
    public DateTime? UtcValue { get; set; }
    public bool DateOnly { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (UtcValue is not { } value)
        {
            output.SuppressOutput();
            return;
        }

        // SQL datetime values have Unspecified Kind but are stored as UTC.
        var utc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        output.Attributes.SetAttribute("datetime", utc.ToString("O", CultureInfo.InvariantCulture));
        output.Attributes.SetAttribute("data-local-time", DateOnly ? "date" : "datetime");
        output.Content.SetContent(utc.ToString(DateOnly ? "dd.MM.yyyy" : "dd.MM.yyyy HH:mm",
            CultureInfo.InvariantCulture) + " UTC");
    }
}
