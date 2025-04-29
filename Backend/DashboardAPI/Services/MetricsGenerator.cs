using DashboardAPI.WebSocket;
using Microsoft.Extensions.Logging;

namespace DashboardAPI.Services
{
    public class MetricsGenerator : BackgroundService
    {
        private readonly IMetricsService _metricsService;
        private readonly WebSocketHandler _webSocketHandler;
        private readonly IRabbitMQService _rabbitMQService;
        private readonly ILogger<MetricsGenerator> _logger;
        private readonly Random _random = new();

        public MetricsGenerator(
            IMetricsService metricsService, 
            WebSocketHandler webSocketHandler,
            IRabbitMQService rabbitMQService,
            ILogger<MetricsGenerator> logger)
        {
            _metricsService = metricsService;
            _webSocketHandler = webSocketHandler;
            _rabbitMQService = rabbitMQService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_rabbitMQService.IsConnected)
            {
                _logger.LogInformation("Starting RabbitMQ consumer");
                // Start consuming messages from RabbitMQ
                _rabbitMQService.StartConsuming(async metrics =>
                {
                    _metricsService.AddMetric(metrics);
                    await _webSocketHandler.BroadcastMetrics(metrics);
                });
            }
            else
            {
                _logger.LogWarning("RabbitMQ is not available. Running in direct mode without message broker.");
            }

            // Generate metrics
            while (!stoppingToken.IsCancellationRequested)
            {
                var metrics = GenerateMetrics();
                
                if (_rabbitMQService.IsConnected)
                {
                    _rabbitMQService.PublishMetrics(metrics);
                }
                else
                {
                    // Direct processing when RabbitMQ is not available
                    _metricsService.AddMetric(metrics);
                    await _webSocketHandler.BroadcastMetrics(metrics);
                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        private SystemMetrics GenerateMetrics()
        {
            return new SystemMetrics
            {
                Timestamp = DateTime.UtcNow,
                CpuUsage = _random.NextDouble() * 100,
                MemoryUsage = _random.NextDouble() * 100,
                ActiveUsers = _random.Next(1, 1000),
                SystemStatus = _random.Next(100) > 90 ? "Warning" : "Normal"
            };
        }
    }
}