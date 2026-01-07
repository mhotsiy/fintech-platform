import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, Link } from 'react-router-dom';
import { 
  ArrowLeft, 
  DollarSign, 
  TrendingUp, 
  TrendingDown,
  Plus,
  CheckCircle,
  XCircle,
  RefreshCw,
  Shield,
  User,
  Download,
  BarChart
} from 'lucide-react';
import { useState } from 'react';
import { merchantsApi, paymentsApi, withdrawalsApi, adminApi } from '@/api/client';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card';
import { Button } from '@/components/Button';
import { Badge } from '@/components/Badge';
import { PageLoading } from '@/components/Loading';
import { ErrorMessage } from '@/components/ErrorMessage';
import RefundModal from '@/components/RefundModal';
import BulkPaymentModal from '@/components/BulkPaymentModal';
import PaymentFilters, { PaymentFilterValues } from '@/components/PaymentFilters';
import { formatCurrency, formatDate, formatRelativeTime, generateIdempotencyKey } from '@/lib/utils';
import type { CreatePaymentRequest, CreateWithdrawalRequest, Payment } from '@/types';

function CreatePaymentModal({ merchantId, isOpen, onClose }: { merchantId: string; isOpen: boolean; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [formData, setFormData] = useState<CreatePaymentRequest>({
    amountInMinorUnits: 10000,
    currency: 'USD',
    description: '',
  });

  const createMutation = useMutation({
    mutationFn: (data: CreatePaymentRequest) => 
      paymentsApi.create(merchantId, data, generateIdempotencyKey()),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['payments', merchantId] });
      queryClient.invalidateQueries({ queryKey: ['balances', merchantId] });
      onClose();
      setFormData({ amountInMinorUnits: 10000, currency: 'USD', description: '' });
    },
  });

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div className="bg-white rounded-lg max-w-md w-full p-6">
        <h2 className="text-xl font-bold text-gray-900 mb-4">Create Payment</h2>
        
        <form
          onSubmit={(e) => {
            e.preventDefault();
            createMutation.mutate(formData);
          }}
          className="space-y-4"
        >
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Amount (in cents)
            </label>
            <input
              type="number"
              value={formData.amountInMinorUnits}
              onChange={(e) => setFormData({ ...formData, amountInMinorUnits: parseInt(e.target.value) })}
              required
              min="1"
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
              placeholder="10000"
            />
            <p className="text-xs text-gray-500 mt-1">
              {formatCurrency(formData.amountInMinorUnits, formData.currency)}
            </p>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Currency
            </label>
            <select
              value={formData.currency}
              onChange={(e) => setFormData({ ...formData, currency: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="USD">USD</option>
              <option value="EUR">EUR</option>
              <option value="GBP">GBP</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Description (optional)
            </label>
            <input
              type="text"
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
              placeholder="Test payment"
            />
          </div>

          {createMutation.error && (
            <ErrorMessage message={(createMutation.error as Error).message} />
          )}

          <div className="flex justify-end space-x-3 pt-4">
            <Button type="button" variant="secondary" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" disabled={createMutation.isPending}>
              {createMutation.isPending ? 'Creating...' : 'Create Payment'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}

function CreateWithdrawalModal({ merchantId, isOpen, onClose }: { merchantId: string; isOpen: boolean; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [formData, setFormData] = useState<CreateWithdrawalRequest>({
    amountInMinorUnits: 5000,
    currency: 'USD',
    bankAccountNumber: '123456789',
    bankRoutingNumber: '987654321',
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateWithdrawalRequest) => 
      withdrawalsApi.create(merchantId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['withdrawals', merchantId] });
      queryClient.invalidateQueries({ queryKey: ['balances', merchantId] });
      queryClient.invalidateQueries({ queryKey: ['ledger', merchantId] });
      onClose();
    },
  });

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div className="bg-white rounded-lg max-w-md w-full p-6">
        <h2 className="text-xl font-bold text-gray-900 mb-4">Request Withdrawal</h2>
        
        <form
          onSubmit={(e) => {
            e.preventDefault();
            createMutation.mutate(formData);
          }}
          className="space-y-4"
        >
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Amount (in cents)
            </label>
            <input
              type="number"
              value={formData.amountInMinorUnits}
              onChange={(e) => setFormData({ ...formData, amountInMinorUnits: parseInt(e.target.value) })}
              required
              min="1"
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
            <p className="text-xs text-gray-500 mt-1">
              {formatCurrency(formData.amountInMinorUnits, formData.currency)}
            </p>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Currency
            </label>
            <select
              value={formData.currency}
              onChange={(e) => setFormData({ ...formData, currency: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            >
              <option value="USD">USD</option>
              <option value="EUR">EUR</option>
              <option value="GBP">GBP</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Bank Account Number
            </label>
            <input
              type="text"
              value={formData.bankAccountNumber}
              onChange={(e) => setFormData({ ...formData, bankAccountNumber: e.target.value })}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Bank Routing Number
            </label>
            <input
              type="text"
              value={formData.bankRoutingNumber}
              onChange={(e) => setFormData({ ...formData, bankRoutingNumber: e.target.value })}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
            />
          </div>

          {createMutation.error && (
            <ErrorMessage message={(createMutation.error as Error).message} />
          )}

          <div className="flex justify-end space-x-3 pt-4">
            <Button type="button" variant="secondary" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" disabled={createMutation.isPending}>
              {createMutation.isPending ? 'Requesting...' : 'Request Withdrawal'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}

export function MerchantDetail() {
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  const [showWithdrawalModal, setShowWithdrawalModal] = useState(false);
  const [showBulkPaymentModal, setShowBulkPaymentModal] = useState(false);
  const [refundingPayment, setRefundingPayment] = useState<Payment | null>(null);
  const [filters, setFilters] = useState<PaymentFilterValues>({});

  const { data: merchant, isLoading: merchantLoading } = useQuery({
    queryKey: ['merchant', id],
    queryFn: () => merchantsApi.getById(id!),
    enabled: !!id,
  });

  const { data: balances, isLoading: balancesLoading } = useQuery({
    queryKey: ['balances', id],
    queryFn: () => merchantsApi.getBalances(id!),
    enabled: !!id,
  });

  const { data: payments, isLoading: paymentsLoading } = useQuery({
    queryKey: ['payments', id, filters],
    queryFn: () => paymentsApi.getByMerchant(id!, filters),
    enabled: !!id,
  });

  const { data: withdrawals } = useQuery({
    queryKey: ['withdrawals', id],
    queryFn: () => withdrawalsApi.getByMerchant(id!),
    enabled: !!id,
  });

  const { data: ledger } = useQuery({
    queryKey: ['ledger', id],
    queryFn: () => adminApi.getLedger(id!),
    enabled: !!id,
  });

  const completePaymentMutation = useMutation({
    mutationFn: ({ paymentId }: { paymentId: string }) => 
      paymentsApi.complete(id!, paymentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['payments', id] });
      queryClient.invalidateQueries({ queryKey: ['balances', id] });
      queryClient.invalidateQueries({ queryKey: ['ledger', id] });
    },
  });

  const handleExportCsv = async () => {
    try {
      const blob = await paymentsApi.exportCsv(id!, filters);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `payments_${id}_${new Date().toISOString()}.csv`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (error) {
      console.error('Failed to export CSV:', error);
    }
  };

  const cancelWithdrawalMutation = useMutation({
    mutationFn: ({ withdrawalId }: { withdrawalId: string }) => 
      withdrawalsApi.cancel(id!, withdrawalId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['withdrawals', id] });
      queryClient.invalidateQueries({ queryKey: ['balances', id] });
      queryClient.invalidateQueries({ queryKey: ['ledger', id] });
    },
  });

  if (merchantLoading || balancesLoading || paymentsLoading) {
    return <PageLoading />;
  }

  if (!merchant) {
    return <ErrorMessage message="Merchant not found" />;
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Link to="/merchants">
            <Button variant="secondary" size="sm">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back
            </Button>
          </Link>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">{merchant.name}</h1>
            <p className="text-sm text-gray-600">{merchant.email}</p>
          </div>
          <Badge status={merchant.status}>{merchant.status}</Badge>
        </div>
        <Link to={`/merchants/${id}/analytics`}>
          <Button variant="secondary">
            <BarChart className="h-4 w-4 mr-2" />
            View Analytics
          </Button>
        </Link>
      </div>

      {/* Balances */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {balances && balances.length > 0 ? (
          balances.map((balance) => (
            <Card key={balance.id}>
              <CardContent className="pt-6">
                <div className="flex items-center justify-between mb-4">
                  <span className="text-sm font-medium text-gray-600">{balance.currency} Balance</span>
                  <DollarSign className="h-5 w-5 text-gray-400" />
                </div>
                <div className="space-y-2">
                  <div>
                    <p className="text-xs text-gray-500">Available</p>
                    <p className="text-2xl font-bold text-gray-900">
                      {formatCurrency(balance.availableBalanceInMinorUnits, balance.currency)}
                    </p>
                  </div>
                  {balance.pendingBalanceInMinorUnits > 0 && (
                    <div>
                      <p className="text-xs text-gray-500">Pending</p>
                      <p className="text-sm font-medium text-yellow-600">
                        {formatCurrency(balance.pendingBalanceInMinorUnits, balance.currency)}
                      </p>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          ))
        ) : (
          <Card>
            <CardContent className="py-8 text-center text-gray-500">
              No balances yet
            </CardContent>
          </Card>
        )}
      </div>

      {/* Actions Row */}
      <div className="flex flex-wrap gap-3">
        <Button onClick={() => setShowPaymentModal(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Create Payment
        </Button>
        <Button onClick={() => setShowBulkPaymentModal(true)} variant="secondary">
          <Plus className="h-4 w-4 mr-2" />
          Bulk Payments
        </Button>
        <Button onClick={() => setShowWithdrawalModal(true)} variant="secondary">
          <TrendingDown className="h-4 w-4 mr-2" />
          Request Withdrawal
        </Button>
        <Button onClick={handleExportCsv} variant="secondary">
          <Download className="h-4 w-4 mr-2" />
          Export CSV
        </Button>
      </div>

      {/* Payments */}
      <PaymentFilters
        onApply={setFilters}
        onReset={() => setFilters({})}
      />
      
      <Card>
        <CardHeader>
          <CardTitle>Payments</CardTitle>
        </CardHeader>
        <CardContent>
          {payments && payments.length > 0 ? (
            <div className="space-y-2">
              {payments.slice(0, 10).map((payment) => (
                <div
                  key={payment.id}
                  className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-lg"
                >
                  <div className="flex items-center space-x-4 flex-1">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <p className="font-medium text-gray-900">
                          {formatCurrency(payment.amountInMinorUnits, payment.currency)}
                        </p>
                        {payment.status === 'Completed' && payment.completedBy === 'FraudDetection' && (
                          <div className="flex items-center gap-1 text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded-full" title="Auto-approved by fraud detection system">
                            <Shield className="h-3 w-3" />
                            <span>Auto-approved</span>
                          </div>
                        )}
                        {payment.status === 'Completed' && payment.completedBy === 'Manual' && (
                          <div className="flex items-center gap-1 text-xs bg-blue-100 text-blue-700 px-2 py-0.5 rounded-full" title="Manually completed by admin">
                            <User className="h-3 w-3" />
                            <span>Manual</span>
                          </div>
                        )}
                      </div>
                      <p className="text-sm text-gray-500">{payment.description || payment.id}</p>
                    </div>
                  </div>
                  <div className="flex items-center space-x-3">
                    <Badge status={payment.status}>{payment.status}</Badge>
                    <span className="text-sm text-gray-500 w-20 text-right">
                      {formatRelativeTime(payment.createdAt)}
                    </span>
                    {payment.status === 'Pending' && (
                      <Button
                        size="sm"
                        onClick={() => completePaymentMutation.mutate({ paymentId: payment.id })}
                        disabled={completePaymentMutation.isPending}
                      >
                        <CheckCircle className="h-4 w-4 mr-1" />
                        Complete
                      </Button>
                    )}
                    {payment.status === 'Completed' && (
                      <Button
                        size="sm"
                        variant="danger"
                        onClick={() => setRefundingPayment(payment)}
                      >
                        <RefreshCw className="h-4 w-4 mr-1" />
                        Refund
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-center text-gray-500 py-8">No payments yet</p>
          )}
        </CardContent>
      </Card>

      {/* Withdrawals */}
      {withdrawals && withdrawals.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Withdrawals</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {withdrawals.map((withdrawal) => (
                <div
                  key={withdrawal.id}
                  className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-lg"
                >
                  <div className="flex items-center space-x-4">
                    <div>
                      <p className="font-medium text-gray-900">
                        {formatCurrency(withdrawal.amountInMinorUnits, withdrawal.currency)}
                      </p>
                      <p className="text-sm text-gray-500">***{withdrawal.bankAccountNumber.slice(-4)}</p>
                    </div>
                  </div>
                  <div className="flex items-center space-x-3">
                    <Badge status={withdrawal.status}>{withdrawal.status}</Badge>
                    <span className="text-sm text-gray-500">
                      {formatRelativeTime(withdrawal.createdAt)}
                    </span>
                    {withdrawal.status === 'Pending' && (
                      <Button
                        size="sm"
                        variant="danger"
                        onClick={() => cancelWithdrawalMutation.mutate({ withdrawalId: withdrawal.id })}
                        disabled={cancelWithdrawalMutation.isPending}
                      >
                        <XCircle className="h-4 w-4 mr-1" />
                        Cancel
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Ledger */}
      {ledger && ledger.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Ledger History</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {ledger.slice(0, 20).map((entry) => (
                <div
                  key={entry.id}
                  className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-lg text-sm"
                >
                  <div className="flex items-center space-x-4 flex-1">
                    {entry.amountInMinorUnits > 0 ? (
                      <TrendingUp className="h-5 w-5 text-green-600" />
                    ) : (
                      <TrendingDown className="h-5 w-5 text-red-600" />
                    )}
                    <div>
                      <p className="font-medium text-gray-900">{entry.entryType}</p>
                      <p className="text-gray-500">{formatDate(entry.createdAt)}</p>
                    </div>
                  </div>
                  <div className="text-right">
                    <p className={`font-medium ${entry.amountInMinorUnits > 0 ? 'text-green-600' : 'text-red-600'}`}>
                      {entry.amountInMinorUnits > 0 ? '+' : ''}
                      {formatCurrency(entry.amountInMinorUnits, entry.currency)}
                    </p>
                    <p className="text-gray-500 text-xs">
                      Balance: {formatCurrency(entry.balanceAfterInMinorUnits, entry.currency)}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      <CreatePaymentModal merchantId={id!} isOpen={showPaymentModal} onClose={() => setShowPaymentModal(false)} />
      <CreateWithdrawalModal merchantId={id!} isOpen={showWithdrawalModal} onClose={() => setShowWithdrawalModal(false)} />
      <BulkPaymentModal merchantId={id!} isOpen={showBulkPaymentModal} onClose={() => setShowBulkPaymentModal(false)} />
      {refundingPayment && (
        <RefundModal
          payment={refundingPayment}
          merchantId={id!}
          onClose={() => setRefundingPayment(null)}
        />
      )}
    </div>
  );
}
