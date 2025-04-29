using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace DashboardAPI.Services
{
    public interface IMetricsService
    {
        void AddMetric(SystemMetrics metrics);
        List<SystemMetrics> GetLatestMetrics(int count = 10);
    }

    public class MetricsService : IMetricsService
    {
        private readonly ConcurrentQueue<SystemMetrics> _metricsCache = new();
        private const int MaxCacheSize = 100;

        public void AddMetric(SystemMetrics metrics)
        {
            _metricsCache.Enqueue(metrics);
            while (_metricsCache.Count > MaxCacheSize)
            {
                _metricsCache.TryDequeue(out _);
            }
        }

        public List<SystemMetrics> GetLatestMetrics(int count = 10)
        {
            return _metricsCache.TakeLast(count).ToList();
        }
    }

    public class SystemMetrics
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonPropertyName("cpuUsage")]
        public double CpuUsage { get; set; }
        
        [JsonPropertyName("memoryUsage")]
        public double MemoryUsage { get; set; }
        
        [JsonPropertyName("activeUsers")]
        public int ActiveUsers { get; set; }
        
        [JsonPropertyName("systemStatus")]
        public string SystemStatus { get; set; } = string.Empty;
    }
}