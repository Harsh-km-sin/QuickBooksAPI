using Azure.Messaging.ServiceBus;
using QuickBooksAPI.Application.Interfaces;
using System.Text;
using System.Text.Json;

namespace QuickBooksAPI.Infrastructure.Queue;

public class GlServiceBusPublisher : IGlQueuePublisher
{
    private readonly ServiceBusSender _sender;

    public GlServiceBusPublisher(ServiceBusSender sender) => _sender = sender;

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);
        var busMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(json));
        await _sender.SendMessageAsync(busMessage, cancellationToken);
    }
}
