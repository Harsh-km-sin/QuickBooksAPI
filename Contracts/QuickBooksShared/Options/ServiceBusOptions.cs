namespace QuickBooksShared.Options;

public class ServiceBusOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = "qbo-full-sync";
}

