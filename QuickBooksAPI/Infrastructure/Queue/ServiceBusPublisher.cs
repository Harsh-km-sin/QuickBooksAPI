using Azure.Messaging.ServiceBus;
using System.Text;
using System.Text.Json;

namespace QuickBooksAPI.Infrastructure.Queue
{
    public class ServiceBusPublisher: IQueuePublisher
    {
        private readonly ServiceBusSender _sender;

        public ServiceBusPublisher(ServiceBusSender sender)
        {
            _sender = sender;
        }

        public async Task PublishAsync<T>(T message)
        {
            var json = JsonSerializer.Serialize(message);

            var busMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(json));

            await _sender.SendMessageAsync(busMessage);
        }
    }
}
