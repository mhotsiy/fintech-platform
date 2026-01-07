import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { paymentsApi } from '../api/client';
import { Button } from './Button';

interface BulkPaymentModalProps {
  merchantId: string;
  isOpen: boolean;
  onClose: () => void;
}

export default function BulkPaymentModal({ merchantId, isOpen, onClose }: BulkPaymentModalProps) {
  const queryClient = useQueryClient();
  const [count, setCount] = useState(10);
  const [amount, setAmount] = useState('100.00');
  const [currency, setCurrency] = useState('USD');
  const [description, setDescription] = useState('Bulk test payment');
  const [error, setError] = useState<string | null>(null);

  if (!isOpen) return null;

  const bulkMutation = useMutation({
    mutationFn: async () => {
      if (count < 1 || count > 1000) {
        throw new Error('Count must be between 1 and 1000');
      }
      if (parseFloat(amount) <= 0) {
        throw new Error('Amount must be greater than 0');
      }

      const amountInMinorUnits = Math.round(parseFloat(amount) * 100);
      const payments = Array.from({ length: count }, (_, i) => ({
        amountInMinorUnits,
        currency,
        description: `${description} #${i + 1}`,
      }));

      await paymentsApi.createBulk(merchantId, payments);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['merchants', merchantId, 'payments'] });
      queryClient.invalidateQueries({ queryKey: ['merchants', merchantId, 'balances'] });
      onClose();
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || err.message || 'Failed to create bulk payments');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    bulkMutation.mutate();
  };

  const totalAmount = (count * parseFloat(amount)).toFixed(2);

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4">
        <div className="p-6">
          <h2 className="text-2xl font-bold mb-4">Create Bulk Payments</h2>
          <p className="text-sm text-gray-600 mb-4">
            Create multiple payments at once for testing purposes. Maximum 1,000 payments.
          </p>

          <form onSubmit={handleSubmit}>
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Number of Payments
              </label>
              <input
                type="number"
                min="1"
                max="1000"
                value={count}
                onChange={(e) => setCount(parseInt(e.target.value) || 1)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                required
              />
              <p className="text-xs text-gray-500 mt-1">Between 1 and 1,000</p>
            </div>

            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Amount per Payment
              </label>
              <input
                type="number"
                step="0.01"
                min="0.01"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                required
              />
            </div>

            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Currency
              </label>
              <select
                value={currency}
                onChange={(e) => setCurrency(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
              >
                <option value="USD">USD</option>
                <option value="EUR">EUR</option>
                <option value="GBP">GBP</option>
              </select>
            </div>

            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Description Template
              </label>
              <input
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                placeholder="Bulk test payment"
              />
              <p className="text-xs text-gray-500 mt-1">Each payment will be numbered (e.g., #1, #2, #3...)</p>
            </div>

            <div className="mb-4 p-3 bg-indigo-50 border border-indigo-200 rounded">
              <p className="text-sm font-semibold text-indigo-900">
                Total: {totalAmount} {currency}
              </p>
              <p className="text-xs text-indigo-700 mt-1">
                {count} payment{count !== 1 ? 's' : ''} × {amount} {currency}
              </p>
            </div>

            {error && (
              <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded text-red-700 text-sm">
                {error}
              </div>
            )}

            <div className="flex space-x-3">
              <Button
                type="button"
                variant="secondary"
                onClick={onClose}
                disabled={bulkMutation.isPending}
                className="flex-1"
              >
                Cancel
              </Button>
              <Button
                type="submit"
                variant="primary"
                disabled={bulkMutation.isPending}
                className="flex-1"
              >
                Create {count} Payment{count !== 1 ? 's' : ''}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
