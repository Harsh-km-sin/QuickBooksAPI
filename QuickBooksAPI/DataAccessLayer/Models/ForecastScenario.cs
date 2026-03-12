namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class ForecastScenario
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public int HorizonMonths { get; set; }
        public string? AssumptionsJson { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
