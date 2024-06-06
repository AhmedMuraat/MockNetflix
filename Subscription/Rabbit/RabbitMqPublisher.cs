using RabbitMQ.Client;
using System.Text;

public class RabbitMqPublisher
{
    private readonly IConnection _connection;
    private readonly RabbitMQ.Client.IModel _channel;

    public RabbitMqPublisher()
    {
        var factory = new ConnectionFactory()
        {
            HostName = "rabbitmq", // Use the service name defined in docker-compose
            UserName = "user",     // Default user from docker-compose
            Password = "password"  // Default password from docker-compose
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public void PublishMessage(string message)
    {
        _channel.QueueDeclare(queue: "subscriptionQueue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var body = Encoding.UTF8.GetBytes(message);

        _channel.BasicPublish(exchange: "",
                             routingKey: "subscriptionQueue",
                             basicProperties: null,
                             body: body);
    }
}
