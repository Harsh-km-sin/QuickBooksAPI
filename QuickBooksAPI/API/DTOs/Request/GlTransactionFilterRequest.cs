namespace QuickBooksAPI.API.DTOs.Request;

public class GlTransactionFilterRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? RiskTier { get; set; }
    public string? AccountName { get; set; }
    public string? EntityName { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public decimal? AmountMin { get; set; }
    public decimal? AmountMax { get; set; }
    public bool UnreviewedOnly { get; set; } = false;
}

public class GlMarkReviewedRequest
{
    public string? Note { get; set; }
}
