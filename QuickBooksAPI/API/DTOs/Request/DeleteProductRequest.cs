namespace QuickBooksAPI.API.DTOs.Request
{
    public class DeleteProductRequest
    {
        public string Id { get; set; }
        public string SyncToken { get; set; }
        public bool Active { get; set; } = false;
        public string Type { get; set; }
        public IncomeAccountRef? IncomeAccountRef { get; set; }
    }

    public class IncomeAccountRef
    {
        public string? Value { get; set; }
        public string? Name { get; set; }
    }
}
