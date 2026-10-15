using System.Globalization;
using System.Text.RegularExpressions;

namespace Marketplace.BLL.Common;

public static class ProductSkuGenerator
{
    public const int MaxLength = 100;

    private const string DefaultPrefix = "SKU-";
    private const int DefaultNumberWidth = 5;
    private static readonly Regex NumberedSkuPattern = new(
        @"^(?<prefix>.*?)(?<number>\d+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Generate(IEnumerable<string?> existingSkus)
    {
        var usedSkus = existingSkus
            .Where(sku => !string.IsNullOrWhiteSpace(sku))
            .Select(sku => sku!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var used = usedSkus.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var numberedSkus = usedSkus
            .Select((sku, index) => ParseNumberedSku(sku, index))
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();

        if (numberedSkus.Count > 0)
        {
            var series = numberedSkus
                .GroupBy(item => item.Prefix, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Min(item => item.RecencyIndex))
                .First();

            var prefix = series.First().Prefix;
            var numberWidth = series.Max(item => item.NumberWidth);
            var nextNumber = series.Max(item => item.Number);

            while (nextNumber < ulong.MaxValue)
            {
                nextNumber++;
                var candidate = prefix + nextNumber.ToString($"D{numberWidth}", CultureInfo.InvariantCulture);
                if (candidate.Length <= MaxLength && used.Add(candidate))
                {
                    return candidate;
                }
            }

            return GenerateFallback(used, prefix);
        }

        var defaultNumber = 1UL;
        while (defaultNumber < ulong.MaxValue)
        {
            var candidate = DefaultPrefix + defaultNumber.ToString($"D{DefaultNumberWidth}", CultureInfo.InvariantCulture);
            if (used.Add(candidate))
            {
                return candidate;
            }

            defaultNumber++;
        }

        return GenerateFallback(used, DefaultPrefix);
    }

    private static NumberedSku? ParseNumberedSku(string sku, int recencyIndex)
    {
        var match = NumberedSkuPattern.Match(sku);
        if (!match.Success ||
            !ulong.TryParse(match.Groups["number"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            return null;
        }

        var prefix = match.Groups["prefix"].Value;
        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = DefaultPrefix;
        }

        return new NumberedSku(prefix, number, match.Groups["number"].Value.Length, recencyIndex);
    }

    private static string GenerateFallback(HashSet<string> used, string preferredPrefix)
    {
        var prefix = string.IsNullOrWhiteSpace(preferredPrefix) ? DefaultPrefix : preferredPrefix;

        while (true)
        {
            var suffix = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            var separator = prefix.EndsWith('-') ? string.Empty : "-";
            var maxPrefixLength = MaxLength - separator.Length - suffix.Length;
            var shortenedPrefix = prefix[..Math.Min(prefix.Length, maxPrefixLength)];
            var candidate = shortenedPrefix + separator + suffix;

            if (used.Add(candidate))
            {
                return candidate;
            }
        }
    }

    private sealed record NumberedSku(string Prefix, ulong Number, int NumberWidth, int RecencyIndex);
}
