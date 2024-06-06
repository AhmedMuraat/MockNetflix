using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

public class RabbitMqConsumer
{
    private readonly IConnection _connection;
    private readonly RabbitMQ.Client.IModel _channel;

    public RabbitMqConsumer()
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

    public void Consume()
    {
        _channel.QueueDeclare(queue: "subscriptionQueue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine(" [x] Received {0}", message);
            // Process the message and update the database accordingly
        };
        _channel.BasicConsume(queue: "subscriptionQueue",
                             autoAck: true,
                             consumer: consumer);
    }
}
