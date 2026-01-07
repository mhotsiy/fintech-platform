// Domain Types
export interface Merchant {
  id: string;
  name: string;
  email: string;
  status: string;
  createdAt: string;
}

export interface Payment {
  id: string;
  merchantId: string;
  amountInMinorUnits: number;
  currency: string;
  status: 'Pending' | 'Completed' | 'Failed' | 'Refunded';
  externalReference?: string;
  description?: string;
  createdAt: string;
  completedAt?: string;
  completedBy?: 'Manual' | 'FraudDetection';
  refundedAt?: string;
  refundReason?: string;
  refundedAmountInMinorUnits?: number;
}

export type PaymentStatus = 'Pending' | 'Completed' | 'Failed' | 'Refunded';

export interface Balance {
  id: string;
  merchantId: string;
  currency: string;
  availableBalanceInMinorUnits: number;
  pendingBalanceInMinorUnits: number;
  totalBalanceInMinorUnits: number;
  lastUpdated: string;
}

export interface Withdrawal {
  id: string;
  merchantId: string;
  amountInMinorUnits: number;
  currency: string;
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed' | 'Cancelled';
  bankAccountNumber: string;
  bankRoutingNumber: string;
  externalTransactionId?: string;
  failureReason?: string;
  createdAt: string;
  processedAt?: string;
  completedAt?: string;
}

export interface LedgerEntry {
  id: string;
  merchantId: string;
  entryType: string;
  amountInMinorUnits: number;
  currency: string;
  balanceAfterInMinorUnits: number;
  relatedPaymentId?: string;
  relatedWithdrawalId?: string;
  description?: string;
  createdAt: string;
}

// Request Types
export interface CreateMerchantRequest {
  name: string;
  email: string;
}

export interface CreatePaymentRequest {
  amountInMinorUnits: number;
  currency: string;
  externalReference?: string;
  description?: string;
}

export interface CreateWithdrawalRequest {
  amountInMinorUnits: number;
  currency: string;
  bankAccountNumber: string;
  bankRoutingNumber: string;
}

// Utility Types
export interface SystemHealth {
  api: boolean;
  workers: boolean;
  database: boolean;
  kafka: boolean;
}

export interface SystemMetrics {
  paymentsCreated: number;
  paymentsCompleted: number;
  withdrawals: number;
  refunds: number;
}
