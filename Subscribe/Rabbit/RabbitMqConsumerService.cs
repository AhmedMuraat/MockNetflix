using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Subscribe.Models;
using System.Text;
using System.Text.Json;

namespace Subscribe.Rabbit
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqConsumerService> _logger;

        public RabbitMqConsumerService(IServiceProvider serviceProvider, IModel channel, ILogger<RabbitMqConsumerService> logger)
        {
            _serviceProvider = serviceProvider;
            _channel = channel;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel.QueueDeclare(queue: "user-delete-subscription-queue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                if (stoppingToken.IsCancellationRequested) return;

                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Received message in user-delete-subscription-queue: {Message}", message);

                try
                {
                    var deleteMessage = JsonSerializer.Deserialize<UserDeleteMessage>(message);
                    if (deleteMessage != null)
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetRequiredService<SubContext>();
                            var userSubscriptions = context.UserSubscriptions.Where(us => us.ExternalUserId == deleteMessage.UserId);
                            context.UserSubscriptions.RemoveRange(userSubscriptions);
                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message in user-delete-subscription-queue: {Message}", message);
                }
            };

            _channel.BasicConsume(queue: "user-delete-subscription-queue",
                                 autoAck: true,
                                 consumer: consumer);

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RabbitMqConsumerService is stopping.");
            _channel?.Dispose();
            return base.StopAsync(stoppingToken);
        }
    }
}
