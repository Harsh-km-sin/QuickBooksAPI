namespace QuickBooksAPI.API.DTOs.Request
{
    /// <summary>
    /// Common query parameters for list endpoints (pagination and search).
    /// </summary>
    public class ListQueryParams
    {
        /// <summary>1-based page number. Default: 1.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Number of items per page. Default: 20, max: 100.</summary>
        public int PageSize { get; set; } = 20;

        /// <summary>Optional search term to filter results.</summary>
        public string? Search { get; set; }

        /// <summary>Filter by active status: "active", "inactive", or null/empty for all.</summary>
        public string? ActiveFilter { get; set; }

        public bool? GetActiveFilter() => ActiveFilter?.ToLower() switch
        {
            "active" => true,
            "inactive" => false,
            _ => null
        };

        public int GetSkip() => Math.Max(0, (Page - 1) * GetPageSize());
        public int GetPageSize() => Math.Clamp(PageSize, 1, 100);
        public int GetPage() => Math.Max(1, Page);
    }
}
