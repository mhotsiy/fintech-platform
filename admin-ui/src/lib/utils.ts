import { type ClassValue, clsx } from 'clsx';

export function cn(...inputs: ClassValue[]) {
  return clsx(inputs);
}

export function formatCurrency(amountInMinorUnits: number, currency: string = 'USD'): string {
  const amount = amountInMinorUnits / 100;
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
  }).format(amount);
}

export function formatDate(dateString: string): string {
  const date = new Date(dateString);
  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

export function formatRelativeTime(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (diffInSeconds < 60) return 'just now';
  if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}min ago`;
  if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}hr ago`;
  return `${Math.floor(diffInSeconds / 86400)}d ago`;
}

export function getStatusColor(status: string): string {
  const statusColors: Record<string, string> = {
    Pending: 'text-yellow-600 bg-yellow-50',
    Processing: 'text-blue-600 bg-blue-50',
    Completed: 'text-green-600 bg-green-50',
    Failed: 'text-red-600 bg-red-50',
    Cancelled: 'text-gray-600 bg-gray-50',
    Refunded: 'text-purple-600 bg-purple-50',
    Active: 'text-green-600 bg-green-50',
  };
  return statusColors[status] || 'text-gray-600 bg-gray-50';
}

export function generateIdempotencyKey(): string {
  return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}
