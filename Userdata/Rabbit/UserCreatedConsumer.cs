using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Userdata.Models;

public class UserCreatedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQ.Client.IModel _channel;
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(IServiceProvider serviceProvider, RabbitMQ.Client.IModel channel, ILogger<UserCreatedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _channel = channel;
        _logger = logger;

        _channel.QueueDeclare(queue: "user.created", durable: false, exclusive: false, autoDelete: false, arguments: null);
        _logger.LogInformation("Queue declared: user.created");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Received message: {Message}", message);

            try
            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new DateOnlyJsonConverter() }
                };
                var userCreatedEvent = JsonSerializer.Deserialize<UserCreatedEvent>(message, options);
                _logger.LogInformation("Deserialized userCreatedEvent for user: {UserId}", userCreatedEvent.UserId);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<UserInfoContext>();

                    var newUser = new UserDatum
                    {
                        UserId = userCreatedEvent.UserId,
                        Name = userCreatedEvent.Name,
                        LastName = userCreatedEvent.LastName,
                        Address = userCreatedEvent.Address,
                        DateOfBirth = userCreatedEvent.DateOfBirth,
                        AccountCreated = userCreatedEvent.AccountCreated
                    };

                    context.UserData.Add(newUser);
                    await context.SaveChangesAsync();

                    _logger.LogInformation("User information saved for user: {UserId}", userCreatedEvent.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", message);
            }
        };

        _channel.BasicConsume(queue: "user.created", autoAck: true, consumer: consumer);

        _logger.LogInformation("RabbitMQ consumer started.");
        return Task.CompletedTask;
    }
}