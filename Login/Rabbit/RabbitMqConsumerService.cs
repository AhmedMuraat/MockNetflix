using Login.Models;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Login.Rabbit
{
    public class RabbitMqConsumerService: BackgroundService
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
            _channel.QueueDeclare(queue: "user-update-queue",
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
                _logger.LogInformation("Received message in user-update-queue: {Message}", message);

                try
                {
                    var updateMessage = JsonSerializer.Deserialize<UserUpdateMessage>(message);
                    if (updateMessage != null)
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetRequiredService<NetflixLoginContext>();
                            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == updateMessage.UserId);
                            if (user != null)
                            {
                                user.Username = updateMessage.Username;
                                user.Email = updateMessage.Email;
                                await context.SaveChangesAsync(stoppingToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message in user-update-queue: {Message}", message);
                }
            };

            _channel.BasicConsume(queue: "user-update-queue",
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
