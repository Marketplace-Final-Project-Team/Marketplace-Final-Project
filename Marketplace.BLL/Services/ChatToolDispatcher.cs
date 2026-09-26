using System.Text.Json;
using Marketplace.BLL.Interfaces;

namespace Marketplace.BLL.Services
{
    public class ChatToolDispatcher
    {
        private readonly IChatToolsService _tools;

        public ChatToolDispatcher(IChatToolsService tools)
        {
            _tools = tools;
        }

        public async Task<object> ExecuteAsync(string functionName, Dictionary<string, object>? args)
        {
            args ??= new Dictionary<string, object>();

            return functionName switch
            {
                "search_products" => await _tools.SearchProductsAsync(
                    GetString(args, "searchTerm"),
                    GetString(args, "categoryName"),
                    GetString(args, "businessName"),
                    GetFloat(args, "minRating"),
                    GetDecimal(args, "minPrice"),
                    GetDecimal(args, "maxPrice"),
                    GetInt(args, "limit") ?? 10),

                "get_categories" => await _tools.GetCategoriesAsync(),

                "get_product_details" => await _tools.GetProductDetailsAsync(
                    GetGuid(args, "productId")
                        ?? throw new ArgumentException("productId is required")) ?? (object)new { error = "Product not found" },

                "compare_products" => await _tools.CompareProductsAsync(
                    GetGuidArray(args, "productIds")),

                "get_product_reviews" => await _tools.GetProductReviewsAsync(
                    GetGuid(args, "productId")
                        ?? throw new ArgumentException("productId is required"),
                    GetInt(args, "limit") ?? 10),

                "get_similar_products" => await _tools.GetSimilarProductsAsync(
                    GetGuid(args, "productId")
                        ?? throw new ArgumentException("productId is required"),
                    GetInt(args, "limit") ?? 5),

                "check_availability" => await _tools.CheckAvailabilityAsync(
                    GetGuid(args, "productId")
                        ?? throw new ArgumentException("productId is required")),

                "get_discounts" => await _tools.GetDiscountsAsync(
                    GetString(args, "categoryName"),
                    GetInt(args, "limit") ?? 10),

                "add_to_cart" => await _tools.AddToCartAsync(
                    GetGuid(args, "productId")
                        ?? throw new ArgumentException("productId is required"),
                    GetInt(args, "quantity") ?? 1),

                "remove_from_cart" => await _tools.RemoveFromCartAsync(
                    GetGuid(args, "productId")
                        ?? throw new ArgumentException("productId is required")),

                _ => throw new ArgumentException($"Unknown function: {functionName}")
            };
        }

        private static string? GetString(Dictionary<string, object> args, string key)
        {
            if (!args.TryGetValue(key, out var value)) return null;
            if (value is JsonElement el && el.ValueKind == JsonValueKind.String) return el.GetString();
            return value.ToString();
        }

        private static int? GetInt(Dictionary<string, object> args, string key)
        {
            if (!args.TryGetValue(key, out var value)) return null;
            if (value is JsonElement el && el.TryGetInt32(out var i)) return i;
            return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
        }

        private static float? GetFloat(Dictionary<string, object> args, string key)
        {
            if (!args.TryGetValue(key, out var value)) return null;
            if (value is JsonElement el && el.TryGetSingle(out var f)) return f;
            return float.TryParse(value.ToString(), out var parsed) ? parsed : null;
        }

        private static decimal? GetDecimal(Dictionary<string, object> args, string key)
        {
            if (!args.TryGetValue(key, out var value)) return null;
            if (value is JsonElement el && el.TryGetDecimal(out var d)) return d;
            return decimal.TryParse(value.ToString(), out var parsed) ? parsed : null;
        }

        private static Guid? GetGuid(Dictionary<string, object> args, string key)
        {
            var str = GetString(args, key);
            return Guid.TryParse(str, out var guid) ? guid : null;
        }

        private static Guid[] GetGuidArray(Dictionary<string, object> args, string key)
        {
            if (!args.TryGetValue(key, out var value)) return Array.Empty<Guid>();

            if (value is JsonElement el && el.ValueKind == JsonValueKind.Array)
            {
                return el.EnumerateArray()
                    .Select(x => Guid.TryParse(x.GetString(), out var g) ? g : (Guid?)null)
                    .Where(g => g.HasValue)
                    .Select(g => g!.Value)
                    .ToArray();
            }

            return Array.Empty<Guid>();
        }
    }
}