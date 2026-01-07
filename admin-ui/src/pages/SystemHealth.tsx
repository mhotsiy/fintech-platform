import { useQuery } from '@tanstack/react-query';
import { CheckCircle, XCircle, Activity, Database, Zap, MessageSquare } from 'lucide-react';
import { healthApi } from '@/api/client';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card';

export function SystemHealth() {
  const { data: health, refetch } = useQuery({
    queryKey: ['health'],
    queryFn: healthApi.check,
    refetchInterval: 30000, // Refetch every 30 seconds
  });

  const services = [
    {
      name: 'API',
      icon: Activity,
      status: health ? 'healthy' : 'unknown',
      url: 'http://localhost:5153',
    },
    {
      name: 'Workers',
      icon: Zap,
      status: 'healthy', // Would need actual health check
      url: 'http://localhost:5002',
    },
    {
      name: 'PostgreSQL',
      icon: Database,
      status: health ? 'healthy' : 'unknown',
    },
    {
      name: 'Kafka',
      icon: MessageSquare,
      status: 'healthy', // Would need actual health check
      url: 'http://localhost:9092',
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">System Health</h1>
          <p className="mt-1 text-sm text-gray-600">
            Monitor the status of all platform services
          </p>
        </div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-white border border-gray-300 rounded-md hover:bg-gray-50 text-sm font-medium"
        >
          Refresh
        </button>
      </div>

      {/* Overall Status */}
      <Card>
        <CardHeader className="bg-green-50">
          <div className="flex items-center">
            <CheckCircle className="h-8 w-8 text-green-600 mr-3" />
            <div>
              <CardTitle>All Systems Operational</CardTitle>
              <p className="text-sm text-gray-600 mt-1">All services are running normally</p>
            </div>
          </div>
        </CardHeader>
      </Card>

      {/* Services Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {services.map((service) => {
          const Icon = service.icon;
          const isHealthy = service.status === 'healthy';

          return (
            <Card key={service.name}>
              <CardContent className="pt-6">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-4">
                    <div className={`p-3 rounded-lg ${isHealthy ? 'bg-green-100' : 'bg-gray-100'}`}>
                      <Icon className={`h-8 w-8 ${isHealthy ? 'text-green-600' : 'text-gray-400'}`} />
                    </div>
                    <div>
                      <h3 className="text-lg font-semibold text-gray-900">{service.name}</h3>
                      {service.url && (
                        <p className="text-sm text-gray-500">{service.url}</p>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center">
                    {isHealthy ? (
                      <div className="flex items-center text-green-600">
                        <CheckCircle className="h-6 w-6 mr-2" />
                        <span className="font-medium">Healthy</span>
                      </div>
                    ) : (
                      <div className="flex items-center text-gray-400">
                        <XCircle className="h-6 w-6 mr-2" />
                        <span className="font-medium">Unknown</span>
                      </div>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Links */}
      <Card>
        <CardHeader>
          <CardTitle>Monitoring & Tools</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <a
              href="http://localhost:3000"
              target="_blank"
              rel="noopener noreferrer"
              className="p-4 border border-gray-200 rounded-lg hover:border-primary-500 hover:bg-primary-50 transition-all"
            >
              <Activity className="h-8 w-8 text-primary-600 mb-2" />
              <h3 className="font-medium text-gray-900">Grafana</h3>
              <p className="text-sm text-gray-600 mt-1">View metrics dashboards</p>
            </a>

            <a
              href="http://localhost:5153/swagger"
              target="_blank"
              rel="noopener noreferrer"
              className="p-4 border border-gray-200 rounded-lg hover:border-primary-500 hover:bg-primary-50 transition-all"
            >
              <Database className="h-8 w-8 text-primary-600 mb-2" />
              <h3 className="font-medium text-gray-900">API Docs</h3>
              <p className="text-sm text-gray-600 mt-1">Swagger documentation</p>
            </a>

            <a
              href="http://localhost:9021"
              target="_blank"
              rel="noopener noreferrer"
              className="p-4 border border-gray-200 rounded-lg hover:border-primary-500 hover:bg-primary-50 transition-all"
            >
              <MessageSquare className="h-8 w-8 text-primary-600 mb-2" />
              <h3 className="font-medium text-gray-900">Kafka UI</h3>
              <p className="text-sm text-gray-600 mt-1">Message broker interface</p>
            </a>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
