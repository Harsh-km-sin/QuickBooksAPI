namespace QuickBooksAPI.API.DTOs.Request
{
    public class CreateForecastRequest
    {
        public string Name { get; set; } = string.Empty;
        public int HorizonMonths { get; set; } = 12;
        public string? AssumptionsJson { get; set; }
    }
}
