namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class ForecastResult
    {
        public int Id { get; set; }
        public int ScenarioId { get; set; }
        public DateTime PeriodStart { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetIncome { get; set; }
        public decimal CashBalance { get; set; }
        public decimal? RunwayMonths { get; set; }
        public string? MetadataJson { get; set; }
    }
}
