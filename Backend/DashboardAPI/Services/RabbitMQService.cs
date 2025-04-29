using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace DashboardAPI.Services
{
    public interface IRabbitMQService
    {
        bool IsConnected { get; }
        void PublishMetrics(SystemMetrics metrics);
        void StartConsuming(Action<SystemMetrics> onMessageReceived);
    }

    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private IConnection? _connection;
        private IModel? _channel;
        private const string QueueName = "metrics_queue";
        private readonly ILogger<RabbitMQService> _logger;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        public RabbitMQService(ILogger<RabbitMQService> logger)
        {
            _logger = logger;
            TryConnect();
        }

        private bool TryConnect()
        {
            try
            {
                var factory = new ConnectionFactory { 
                    HostName = "localhost",
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
                };
                
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(queue: QueueName,
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

                _isConnected = true;
                _logger.LogInformation("Successfully connected to RabbitMQ");
                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger.LogWarning("Failed to connect to RabbitMQ: {Message}. The application will continue without message broker functionality.", ex.Message);
                return false;
            }
        }

        public void PublishMetrics(SystemMetrics metrics)
        {
            if (!_isConnected)
            {
                return;
            }

            try
            {
                var message = JsonSerializer.Serialize(metrics);
                var body = Encoding.UTF8.GetBytes(message);

                _channel?.BasicPublish(exchange: "",
                                    routingKey: QueueName,
                                    basicProperties: null,
                                    body: body);
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger.LogError("Error publishing message to RabbitMQ: {Message}", ex.Message);
            }
        }

        public void StartConsuming(Action<SystemMetrics> onMessageReceived)
        {
            if (!_isConnected || _channel == null)
            {
                return;
            }

            try
            {
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var metrics = JsonSerializer.Deserialize<SystemMetrics>(message);

                        if (metrics != null)
                        {
                            onMessageReceived(metrics);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error processing RabbitMQ message: {Message}", ex.Message);
                    }
                };

                _channel.BasicConsume(queue: QueueName,
                                    autoAck: true,
                                    consumer: consumer);
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger.LogError("Error starting RabbitMQ consumer: {Message}", ex.Message);
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
} 