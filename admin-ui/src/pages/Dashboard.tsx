import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Building2, Plus, TrendingUp, DollarSign, ArrowUpRight, Shield, User } from 'lucide-react';
import { merchantsApi, paymentsApi } from '@/api/client';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card';
import { LoadingSpinner } from '@/components/Loading';
import { ErrorMessage } from '@/components/ErrorMessage';
import { formatCurrency, formatRelativeTime } from '@/lib/utils';
import { Badge } from '@/components/Badge';

export function Dashboard() {
  const { data: merchants, isLoading: merchantsLoading, error: merchantsError } = useQuery({
    queryKey: ['merchants'],
    queryFn: merchantsApi.getAll,
  });

  // Get recent payments from all merchants
  const merchantIds = merchants?.map(m => m.id) || [];
  const paymentQueries = useQuery({
    queryKey: ['allPayments', merchantIds],
    queryFn: async () => {
      if (merchantIds.length === 0) return [];
      const payments = await Promise.all(
        merchantIds.slice(0, 5).map(id => paymentsApi.getByMerchant(id).catch(() => []))
      );
      return payments.flat().sort((a, b) => 
        new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
      ).slice(0, 10);
    },
    enabled: merchantIds.length > 0,
  });

  if (merchantsLoading) return <LoadingSpinner />;
  if (merchantsError) return <ErrorMessage message="Failed to load dashboard data" />;

  const totalMerchants = merchants?.length || 0;
  const activeMerchants = merchants?.filter(m => m.status === 'Active').length || 0;

  // Create merchant lookup map
  const merchantMap = new Map(merchants?.map(m => [m.id, m]) || []);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
          <p className="mt-1 text-sm text-gray-600">
            Overview of your fintech platform
          </p>
        </div>
        <Link
          to="/merchants?create=true"
          className="inline-flex items-center px-4 py-2 bg-primary-600 text-white rounded-md hover:bg-primary-700"
        >
          <Plus className="h-4 w-4 mr-2" />
          New Merchant
        </Link>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Total Merchants</p>
                <p className="mt-2 text-3xl font-bold text-gray-900">{totalMerchants}</p>
              </div>
              <Building2 className="h-12 w-12 text-primary-600 opacity-20" />
            </div>
            <p className="mt-4 text-sm text-gray-600">
              {activeMerchants} active
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Total Payments</p>
                <p className="mt-2 text-3xl font-bold text-gray-900">
                  {paymentQueries.data?.length || 0}
                </p>
              </div>
              <DollarSign className="h-12 w-12 text-green-600 opacity-20" />
            </div>
            <p className="mt-4 text-sm text-green-600 flex items-center">
              <TrendingUp className="h-4 w-4 mr-1" />
              Recent activity
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Completed Today</p>
                <p className="mt-2 text-3xl font-bold text-gray-900">
                  {paymentQueries.data?.filter(p => p.status === 'Completed').length || 0}
                </p>
              </div>
              <ArrowUpRight className="h-12 w-12 text-blue-600 opacity-20" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Pending</p>
                <p className="mt-2 text-3xl font-bold text-gray-900">
                  {paymentQueries.data?.filter(p => p.status === 'Pending').length || 0}
                </p>
              </div>
              <DollarSign className="h-12 w-12 text-yellow-600 opacity-20" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Recent Activity */}
      <Card>
        <CardHeader>
          <CardTitle>Recent Payments</CardTitle>
        </CardHeader>
        <CardContent>
          {paymentQueries.isLoading ? (
            <LoadingSpinner />
          ) : paymentQueries.data && paymentQueries.data.length > 0 ? (
            <div className="space-y-3">
              {paymentQueries.data.map((payment) => {
                const merchant = merchantMap.get(payment.merchantId);
                return (
                  <Link
                    key={payment.id}
                    to={`/merchants/${payment.merchantId}`}
                    className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-lg transition-colors cursor-pointer border border-transparent hover:border-primary-200"
                    data-testid={`recent-payment-${payment.id}`}
                  >
                    <div className="flex items-center space-x-4 flex-1">
                      <div className="flex-shrink-0">
                        <div className="w-10 h-10 rounded-full bg-primary-100 flex items-center justify-center">
                          <Building2 className="h-5 w-5 text-primary-600" />
                        </div>
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <p className="text-sm font-medium text-gray-900">
                            {formatCurrency(payment.amountInMinorUnits, payment.currency)}
                          </p>
                          <span className="text-gray-400">•</span>
                          <p className="text-sm font-medium text-primary-600 truncate">
                            {merchant?.name || 'Unknown Merchant'}
                          </p>
                          {payment.status === 'Completed' && payment.completedBy === 'FraudDetection' && (
                            <div className="flex items-center gap-1 text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded-full" title="Auto-approved by fraud detection">
                              <Shield className="h-3 w-3" />
                            </div>
                          )}
                          {payment.status === 'Completed' && payment.completedBy === 'Manual' && (
                            <div className="flex items-center gap-1 text-xs bg-blue-100 text-blue-700 px-2 py-0.5 rounded-full" title="Manually completed">
                              <User className="h-3 w-3" />
                            </div>
                          )}
                        </div>
                        <p className="text-sm text-gray-500 truncate">
                          {payment.description || payment.id}
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center space-x-4">
                      <Badge status={payment.status}>{payment.status}</Badge>
                      <span className="text-sm text-gray-500 min-w-20 text-right">
                        {formatRelativeTime(payment.createdAt)}
                      </span>
                    </div>
                  </Link>
                );
              })}
            </div>
          ) : (
            <p className="text-center text-gray-500 py-8">No recent payments</p>
          )}
        </CardContent>
      </Card>

      {/* Quick Actions */}
      <Card>
        <CardHeader>
          <CardTitle>Quick Actions</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Link
              to="/merchants"
              className="p-4 border border-gray-200 rounded-lg hover:border-primary-500 hover:bg-primary-50 transition-all"
            >
              <Building2 className="h-8 w-8 text-primary-600 mb-2" />
              <h3 className="font-medium text-gray-900">View Merchants</h3>
              <p className="text-sm text-gray-600 mt-1">Manage merchant accounts</p>
            </Link>
            
            <a
              href="http://localhost:5153/swagger"
              target="_blank"
              rel="noopener noreferrer"
              className="p-4 border border-gray-200 rounded-lg hover:border-primary-500 hover:bg-primary-50 transition-all"
            >
              <ArrowUpRight className="h-8 w-8 text-primary-600 mb-2" />
              <h3 className="font-medium text-gray-900">API Documentation</h3>
              <p className="text-sm text-gray-600 mt-1">View Swagger docs</p>
            </a>
            
            <Link
              to="/health"
              className="p-4 border border-gray-200 rounded-lg hover:border-primary-500 hover:bg-primary-50 transition-all"
            >
              <TrendingUp className="h-8 w-8 text-primary-600 mb-2" />
              <h3 className="font-medium text-gray-900">System Health</h3>
              <p className="text-sm text-gray-600 mt-1">Check service status</p>
            </Link>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
