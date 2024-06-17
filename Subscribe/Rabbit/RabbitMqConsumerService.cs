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
            _channel.QueueDeclare(queue: "buy-credits-queue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            _channel.QueueDeclare(queue: "subscribe-queue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var creditsConsumer = new EventingBasicConsumer(_channel);
            creditsConsumer.Received += async (model, ea) =>
            {
                if (stoppingToken.IsCancellationRequested) return;

                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Received message in buy-credits-queue: {Message}", message);

                try
                {
                    var credits = JsonSerializer.Deserialize<Credit>(message);
                    if (credits != null)
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetRequiredService<SubContext>();
                            context.Credits.Add(credits);
                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message in buy-credits-queue: {Message}", message);
                }
            };

            var subscribeConsumer = new EventingBasicConsumer(_channel);
            subscribeConsumer.Received += async (model, ea) =>
            {
                if (stoppingToken.IsCancellationRequested) return;

                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Received message in subscribe-queue: {Message}", message);

                try
                {
                    var userSubscribe = JsonSerializer.Deserialize<UserSubscription>(message);
                    if (userSubscribe != null)
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetRequiredService<SubContext>();
                            context.UserSubscriptions.Add(userSubscribe);
                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message in subscribe-queue: {Message}", message);
                }
            };

            _channel.BasicConsume(queue: "buy-credits-queue",
                                 autoAck: true,
                                 consumer: creditsConsumer);

            _channel.BasicConsume(queue: "subscribe-queue",
                                 autoAck: true,
                                 consumer: subscribeConsumer);

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
