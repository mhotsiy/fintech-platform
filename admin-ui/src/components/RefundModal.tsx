import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { paymentsApi } from '../api/client';
import { Button } from './Button';
import { Payment } from '../types';

interface RefundModalProps {
  payment: Payment;
  merchantId: string;
  onClose: () => void;
}

export default function RefundModal({ payment, merchantId, onClose }: RefundModalProps) {
  const queryClient = useQueryClient();
  const [refundAmount, setRefundAmount] = useState('');
  const [reason, setReason] = useState('');
  const [isPartialRefund, setIsPartialRefund] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const maxRefundAmount = payment.amountInMinorUnits - (payment.refundedAmountInMinorUnits || 0);
  const maxRefundDisplay = (maxRefundAmount / 100).toFixed(2);

  const refundMutation = useMutation({
    mutationFn: async () => {
      const refundAmountInMinorUnits = isPartialRefund
        ? Math.round(parseFloat(refundAmount) * 100)
        : null;

      // Validation
      if (isPartialRefund) {
        if (!refundAmount || parseFloat(refundAmount) <= 0) {
          throw new Error('Please enter a valid refund amount');
        }
        if (refundAmountInMinorUnits! > maxRefundAmount) {
          throw new Error(`Refund amount cannot exceed ${maxRefundDisplay} ${payment.currency}`);
        }
      }

      await paymentsApi.refund(merchantId, payment.id, {
        refundAmountInMinorUnits: refundAmountInMinorUnits || undefined,
        reason: reason.trim() || undefined,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['merchants', merchantId, 'payments'] });
      queryClient.invalidateQueries({ queryKey: ['merchants', merchantId, 'balances'] });
      onClose();
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || err.message || 'Failed to process refund');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    refundMutation.mutate();
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4">
        <div className="p-6">
          <h2 className="text-2xl font-bold mb-4">Refund Payment</h2>
          
          <div className="mb-4 p-3 bg-gray-50 rounded">
            <p className="text-sm text-gray-600">Payment ID: {payment.id}</p>
            <p className="text-sm text-gray-600">
              Original Amount: {(payment.amountInMinorUnits / 100).toFixed(2)} {payment.currency}
            </p>
            {payment.refundedAmountInMinorUnits && payment.refundedAmountInMinorUnits > 0 && (
              <p className="text-sm text-orange-600">
                Already Refunded: {(payment.refundedAmountInMinorUnits / 100).toFixed(2)} {payment.currency}
              </p>
            )}
            <p className="text-sm font-semibold text-gray-900">
              Max Refund: {maxRefundDisplay} {payment.currency}
            </p>
          </div>

          <form onSubmit={handleSubmit}>
            <div className="mb-4">
              <label className="flex items-center space-x-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={isPartialRefund}
                  onChange={(e) => setIsPartialRefund(e.target.checked)}
                  className="rounded border-gray-300"
                />
                <span className="text-sm font-medium">Partial Refund</span>
              </label>
            </div>

            {isPartialRefund && (
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Refund Amount ({payment.currency})
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0.01"
                  max={maxRefundDisplay}
                  value={refundAmount}
                  onChange={(e) => setRefundAmount(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  placeholder="0.00"
                  required={isPartialRefund}
                />
              </div>
            )}

            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Reason (Optional)
              </label>
              <textarea
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                rows={3}
                placeholder="e.g., Customer dissatisfaction, duplicate charge..."
              />
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
                disabled={refundMutation.isPending}
                className="flex-1"
              >
                Cancel
              </Button>
              <Button
                type="submit"
                variant="danger"
                disabled={refundMutation.isPending}
                className="flex-1"
              >
                {isPartialRefund ? 'Refund Partial' : 'Refund Full Amount'}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
