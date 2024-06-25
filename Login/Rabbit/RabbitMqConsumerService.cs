using Login.Models;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Login.Rabbit
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
            // Declare queues
            _channel.QueueDeclare(queue: "user-update-queue",
                                  durable: false,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);

            _channel.QueueDeclare(queue: "user-delete-login-queue",
                                  durable: false,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);

            // Consumer for user updates
            var updateConsumer = new AsyncEventingBasicConsumer(_channel);
            updateConsumer.Received += async (model, ea) =>
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

            // Consumer for user deletions
            var deleteConsumer = new AsyncEventingBasicConsumer(_channel);
            deleteConsumer.Received += async (model, ea) =>
            {
                if (stoppingToken.IsCancellationRequested) return;

                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Received message in user-delete-login-queue: {Message}", message);

                try
                {
                    var deleteMessage = JsonSerializer.Deserialize<UserDeleteMessage>(message);
                    if (deleteMessage != null)
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetRequiredService<NetflixLoginContext>();
                            var user = await context.Users.Include(u => u.RefreshTokens)
                                                          .FirstOrDefaultAsync(u => u.Id == deleteMessage.UserId);
                            if (user != null)
                            {
                                context.RefreshTokens.RemoveRange(user.RefreshTokens); // Delete related refresh tokens
                                context.Users.Remove(user); // Delete the user
                                await context.SaveChangesAsync(stoppingToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message in user-delete-login-queue: {Message}", message);
                }
            };

            // Register consumers
            _channel.BasicConsume(queue: "user-update-queue",
                                  autoAck: true,
                                  consumer: updateConsumer);

            _channel.BasicConsume(queue: "user-delete-login-queue",
                                  autoAck: true,
                                  consumer: deleteConsumer);

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
