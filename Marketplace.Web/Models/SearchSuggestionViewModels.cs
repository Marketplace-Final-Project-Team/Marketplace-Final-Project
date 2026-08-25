namespace Marketplace.Web.Models
{
    public class SearchSuggestionItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class SearchSuggestionsResultViewModel
    {
        public string Term { get; set; } = string.Empty;
        public string? CorrectedTerm { get; set; }
        public List<SearchSuggestionItemViewModel> Items { get; set; } = new();
    }
}
