import { useQuery } from '@tanstack/react-query';
import { useParams, Link } from 'react-router-dom';
import { ArrowLeft, TrendingUp, DollarSign, CheckCircle, Clock } from 'lucide-react';
import { useState } from 'react';
import {
  LineChart,
  Line,
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { analyticsApi, merchantsApi } from '@/api/client';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card';
import { Button } from '@/components/Button';
import { PageLoading } from '@/components/Loading';
import { ErrorMessage } from '@/components/ErrorMessage';

const STATUS_COLORS: Record<string, string> = {
  Completed: '#10b981',
  Pending: '#f59e0b',
  Failed: '#ef4444',
  Refunded: '#8b5cf6',
};

export function Analytics() {
  const { id } = useParams<{ id: string }>();
  const [dateRange, setDateRange] = useState<'7d' | '30d' | '90d' | 'custom'>('30d');
  const [customFromDate, setCustomFromDate] = useState('');
  const [customToDate, setCustomToDate] = useState('');

  const getDateRange = () => {
    const now = new Date();
    const toDate = now.toISOString().split('T')[0];
    let fromDate: string;

    if (dateRange === 'custom') {
      fromDate = customFromDate || new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
      return { fromDate, toDate: customToDate || toDate };
    }

    const days = dateRange === '7d' ? 7 : dateRange === '30d' ? 30 : 90;
    fromDate = new Date(now.getTime() - days * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
    return { fromDate, toDate };
  };

  const { fromDate, toDate } = getDateRange();

  const { data: merchant } = useQuery({
    queryKey: ['merchant', id],
    queryFn: () => merchantsApi.getById(id!),
    enabled: !!id,
  });

  const { data: analytics, isLoading } = useQuery({
    queryKey: ['analytics', id, fromDate, toDate],
    queryFn: () => analyticsApi.getMerchantAnalytics(id!, fromDate, toDate),
    enabled: !!id,
  });

  if (isLoading) {
    return <PageLoading />;
  }

  if (!analytics) {
    return <ErrorMessage message="Failed to load analytics" />;
  }

  const pieData = analytics.statusDistribution.map((item: any) => ({
    name: item.status,
    value: item.count,
    percentage: item.percentage,
  }));

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Link to={`/merchants/${id}`}>
            <Button variant="secondary" size="sm">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Merchant
            </Button>
          </Link>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Analytics</h1>
            {merchant && <p className="text-sm text-gray-600">{merchant.name}</p>}
          </div>
        </div>
      </div>

      {/* Date Range Selector */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex flex-wrap items-end gap-4">
            <div className="flex gap-2">
              <Button
                variant={dateRange === '7d' ? 'primary' : 'secondary'}
                size="sm"
                onClick={() => setDateRange('7d')}
              >
                Last 7 Days
              </Button>
              <Button
                variant={dateRange === '30d' ? 'primary' : 'secondary'}
                size="sm"
                onClick={() => setDateRange('30d')}
              >
                Last 30 Days
              </Button>
              <Button
                variant={dateRange === '90d' ? 'primary' : 'secondary'}
                size="sm"
                onClick={() => setDateRange('90d')}
              >
                Last 90 Days
              </Button>
              <Button
                variant={dateRange === 'custom' ? 'primary' : 'secondary'}
                size="sm"
                onClick={() => setDateRange('custom')}
              >
                Custom
              </Button>
            </div>
            
            {dateRange === 'custom' && (
              <div className="flex gap-2">
                <input
                  type="date"
                  value={customFromDate}
                  onChange={(e) => setCustomFromDate(e.target.value)}
                  className="px-3 py-1.5 border border-gray-300 rounded-md text-sm"
                />
                <span className="self-center text-gray-500">to</span>
                <input
                  type="date"
                  value={customToDate}
                  onChange={(e) => setCustomToDate(e.target.value)}
                  className="px-3 py-1.5 border border-gray-300 rounded-md text-sm"
                />
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-medium text-gray-600">Total Revenue</span>
              <DollarSign className="h-5 w-5 text-green-600" />
            </div>
            <p className="text-2xl font-bold text-gray-900">
              ${analytics.totalRevenue.toFixed(2)}
            </p>
            <p className="text-xs text-gray-500 mt-1">{analytics.totalPayments} payments</p>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-medium text-gray-600">Success Rate</span>
              <CheckCircle className="h-5 w-5 text-green-600" />
            </div>
            <p className="text-2xl font-bold text-gray-900">
              {analytics.successRate.toFixed(1)}%
            </p>
            <p className="text-xs text-gray-500 mt-1">
              {analytics.completedPayments} of {analytics.totalPayments}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-medium text-gray-600">Avg Payment</span>
              <TrendingUp className="h-5 w-5 text-indigo-600" />
            </div>
            <p className="text-2xl font-bold text-gray-900">
              ${analytics.averagePaymentAmount.toFixed(2)}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-medium text-gray-600">Pending</span>
              <Clock className="h-5 w-5 text-yellow-600" />
            </div>
            <p className="text-2xl font-bold text-gray-900">{analytics.pendingPayments}</p>
            <p className="text-xs text-gray-500 mt-1">
              {analytics.refundedPayments} refunded
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Daily Revenue Chart */}
      <Card>
        <CardHeader>
          <CardTitle>Daily Revenue</CardTitle>
        </CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={analytics.dailyRevenue}>
              <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
              <XAxis
                dataKey="date"
                tickFormatter={(date) => new Date(date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                stroke="#6b7280"
              />
              <YAxis stroke="#6b7280" tickFormatter={(value) => `$${value.toFixed(0)}`} />
              <Tooltip
                formatter={(value: number) => [`$${value.toFixed(2)}`, 'Revenue']}
                labelFormatter={(label) => new Date(label).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })}
              />
              <Legend />
              <Line
                type="monotone"
                dataKey="revenue"
                stroke="#4f46e5"
                strokeWidth={2}
                dot={{ fill: '#4f46e5', r: 4 }}
                activeDot={{ r: 6 }}
                name="Revenue"
              />
            </LineChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Payment Count by Day */}
        <Card>
          <CardHeader>
            <CardTitle>Daily Payment Volume</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={250}>
              <BarChart data={analytics.dailyRevenue}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                <XAxis
                  dataKey="date"
                  tickFormatter={(date) => new Date(date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                  stroke="#6b7280"
                />
                <YAxis stroke="#6b7280" />
                <Tooltip
                  labelFormatter={(label) => new Date(label).toLocaleDateString()}
                />
                <Bar dataKey="count" fill="#4f46e5" name="Payments" />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        {/* Status Distribution */}
        <Card>
          <CardHeader>
            <CardTitle>Status Distribution</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={250}>
              <PieChart>
                <Pie
                  data={pieData}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ name, percentage }) => `${name} (${percentage.toFixed(1)}%)`}
                  outerRadius={80}
                  fill="#8884d8"
                  dataKey="value"
                >
                  {pieData.map((entry: any, index: number) => (
                    <Cell key={`cell-${index}`} fill={STATUS_COLORS[entry.name] || '#94a3b8'} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
            
            {/* Legend */}
            <div className="mt-4 grid grid-cols-2 gap-2">
              {analytics.statusDistribution.map((item: any) => (
                <div key={item.status} className="flex items-center space-x-2">
                  <div
                    className="w-3 h-3 rounded-full"
                    style={{ backgroundColor: STATUS_COLORS[item.status] || '#94a3b8' }}
                  />
                  <span className="text-sm text-gray-700">
                    {item.status}: {item.count} ({item.percentage.toFixed(1)}%)
                  </span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
