import React, { useEffect, useState } from 'react';
import { Line } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
} from 'chart.js';
import { SystemMetrics } from '../types/SystemMetrics';
import './Dashboard.css';

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

export const Dashboard: React.FC = () => {
  const [metrics, setMetrics] = useState<SystemMetrics[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<string>('Connecting...');

  useEffect(() => {
    const ws = new WebSocket('ws://localhost:5153/ws');

    ws.onopen = () => {
      console.log('Connected to WebSocket');
      setConnectionStatus('Connected');
    };

    ws.onmessage = (event) => {
      try {
        const newMetric = JSON.parse(event.data);
        console.log('Raw timestamp:', newMetric.timestamp);
        
        // Convert .NET DateTime to JavaScript Date
        if (newMetric.timestamp) {
          newMetric.timestamp = new Date(newMetric.timestamp).toISOString();
        }
        
        setMetrics(prev => {
          const updated = [...prev, newMetric];
          return updated.slice(-20);
        });
      } catch (error) {
        console.error('Error processing message:', error);
      }
    };

    ws.onerror = (error) => {
      console.error('WebSocket error:', error);
      setConnectionStatus('Connection Error');
    };

    ws.onclose = () => {
      console.log('WebSocket connection closed');
      setConnectionStatus('Disconnected');
    };

    return () => {
      ws.close();
    };
  }, []);

  const formatTime = (timestamp: string) => {
    try {
      const date = new Date(timestamp);
      if (isNaN(date.getTime())) {
        console.error('Invalid timestamp:', timestamp);
        return 'Invalid';
      }
      return date.toLocaleTimeString([], { 
        hour: '2-digit', 
        minute: '2-digit', 
        second: '2-digit',
        hour12: false 
      });
    } catch (error) {
      console.error('Error formatting time:', error);
      return 'Invalid';
    }
  };

  const chartData = {
    labels: metrics.map(m => formatTime(m.timestamp)),
    datasets: [
      {
        label: 'CPU Usage (%)',
        data: metrics.map(m => m.cpuUsage),
        borderColor: '#4ade80',
        backgroundColor: 'rgba(74, 222, 128, 0.1)',
        tension: 0.4,
        fill: true
      },
      {
        label: 'Memory Usage (%)',
        data: metrics.map(m => m.memoryUsage),
        borderColor: '#fbbf24',
        backgroundColor: 'rgba(251, 191, 36, 0.1)',
        tension: 0.4,
        fill: true
      }
    ]
  };

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'top' as const,
        labels: {
          color: '#e0e0e0',
          font: {
            size: 12,
            family: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif'
          }
        }
      },
      title: {
        display: true,
        text: 'System Metrics',
        color: '#e0e0e0',
        font: {
          size: 16,
          family: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
          weight: 500
        }
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        max: 100,
        grid: {
          color: 'rgba(255, 255, 255, 0.1)'
        },
        ticks: {
          color: '#94a3b8',
          font: {
            size: 12
          }
        }
      },
      x: {
        grid: {
          color: 'rgba(255, 255, 255, 0.1)'
        },
        ticks: {
          color: '#94a3b8',
          font: {
            size: 12
          }
        }
      }
    },
    animation: {
      duration: 0
    }
  };

  return (
    <div className="dashboard">
      <div className="connection-status">
        Status: {connectionStatus}
      </div>
      <div className="metrics-container">
        <div className="metric-card">
          <h3>Active Users</h3>
          <p>{metrics[metrics.length - 1]?.activeUsers || 0}</p>
        </div>
        <div className="metric-card">
          <h3>System Status</h3>
          <p className={metrics[metrics.length - 1]?.systemStatus === 'Warning' ? 'warning' : 'normal'}>
            {metrics[metrics.length - 1]?.systemStatus || 'N/A'}
          </p>
        </div>
      </div>
      <div className="chart-container">
        <Line data={chartData} options={options} />
      </div>
    </div>
  );
};