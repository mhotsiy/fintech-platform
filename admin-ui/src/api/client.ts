import axios from 'axios';
import type {
  Merchant,
  Payment,
  Balance,
  Withdrawal,
  LedgerEntry,
  CreateMerchantRequest,
  CreatePaymentRequest,
  CreateWithdrawalRequest,
} from '@/types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5153';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Merchants API
export const merchantsApi = {
  getAll: async (): Promise<Merchant[]> => {
    const { data } = await apiClient.get<Merchant[]>('/api/merchants');
    return data;
  },

  getById: async (id: string): Promise<Merchant> => {
    const { data } = await apiClient.get<Merchant>(`/api/merchants/${id}`);
    return data;
  },

  create: async (request: CreateMerchantRequest): Promise<Merchant> => {
    const { data } = await apiClient.post<Merchant>('/api/merchants', request);
    return data;
  },

  getBalances: async (merchantId: string): Promise<Balance[]> => {
    const { data } = await apiClient.get<Balance[]>(`/api/merchants/${merchantId}/balances`);
    return data;
  },

  getBalance: async (merchantId: string, currency: string): Promise<Balance> => {
    const { data } = await apiClient.get<Balance>(`/api/merchants/${merchantId}/balances/${currency}`);
    return data;
  },
};

// Payments API
export const paymentsApi = {
  getByMerchant: async (merchantId: string, params?: {
    dateFrom?: string;
    dateTo?: string;
    minAmount?: number;
    maxAmount?: number;
    status?: string;
    search?: string;
  }): Promise<Payment[]> => {
    const { data} = await apiClient.get<Payment[]>(`/api/merchants/${merchantId}/payments`, { params });
    return data;
  },

  getById: async (merchantId: string, paymentId: string): Promise<Payment> => {
    const { data } = await apiClient.get<Payment>(`/api/merchants/${merchantId}/payments/${paymentId}`);
    return data;
  },

  create: async (merchantId: string, request: CreatePaymentRequest, idempotencyKey?: string): Promise<Payment> => {
    const headers = idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : {};
    const { data } = await apiClient.post<Payment>(
      `/api/merchants/${merchantId}/payments`,
      request,
      { headers }
    );
    return data;
  },

  createBulk: async (merchantId: string, payments: CreatePaymentRequest[]): Promise<Payment[]> => {
    const { data } = await apiClient.post<Payment[]>(
      `/api/merchants/${merchantId}/payments/bulk`,
      { payments }
    );
    return data;
  },

  complete: async (merchantId: string, paymentId: string): Promise<Payment> => {
    const { data } = await apiClient.post<Payment>(`/api/merchants/${merchantId}/payments/${paymentId}/complete`);
    return data;
  },

  refund: async (merchantId: string, paymentId: string, refundRequest?: {
    refundAmountInMinorUnits?: number;
    reason?: string;
  }): Promise<Payment> => {
    const { data } = await apiClient.post<Payment>(
      `/api/merchants/${merchantId}/payments/${paymentId}/refund`,
      refundRequest || {}
    );
    return data;
  },

  exportCsv: async (merchantId: string, params?: {
    dateFrom?: string;
    dateTo?: string;
    minAmount?: number;
    maxAmount?: number;
    status?: string;
    search?: string;
  }): Promise<Blob> => {
    const { data } = await apiClient.get(`/api/merchants/${merchantId}/payments/export`, {
      params,
      responseType: 'blob'
    });
    return data;
  },
};

// Withdrawals API
export const withdrawalsApi = {
  getByMerchant: async (merchantId: string): Promise<Withdrawal[]> => {
    const { data } = await apiClient.get<Withdrawal[]>(`/api/merchants/${merchantId}/withdrawals`);
    return data;
  },

  getById: async (merchantId: string, withdrawalId: string): Promise<Withdrawal> => {
    const { data } = await apiClient.get<Withdrawal>(`/api/merchants/${merchantId}/withdrawals/${withdrawalId}`);
    return data;
  },

  create: async (merchantId: string, request: CreateWithdrawalRequest): Promise<Withdrawal> => {
    const { data } = await apiClient.post<Withdrawal>(`/api/merchants/${merchantId}/withdrawals`, request);
    return data;
  },

  cancel: async (merchantId: string, withdrawalId: string): Promise<Withdrawal> => {
    const { data } = await apiClient.post<Withdrawal>(`/api/merchants/${merchantId}/withdrawals/${withdrawalId}/cancel`);
    return data;
  },

  process: async (merchantId: string, withdrawalId: string): Promise<Withdrawal> => {
    const { data } = await apiClient.post<Withdrawal>(`/api/merchants/${merchantId}/withdrawals/${withdrawalId}/process`);
    return data;
  },
};

// Admin API
export const adminApi = {
  getLedger: async (merchantId: string): Promise<LedgerEntry[]> => {
    const { data } = await apiClient.get<{ entries: LedgerEntry[] }>(`/api/admin/ledger-history/${merchantId}`);
    return data.entries;
  },

  verifyBalance: async (merchantId: string, currency: string = 'USD'): Promise<any> => {
    const { data } = await apiClient.get(`/api/admin/verify-balance/${merchantId}?currency=${currency}`);
    return data;
  },
};

// Analytics API
export const analyticsApi = {
  getMerchantAnalytics: async (merchantId: string, fromDate?: string, toDate?: string): Promise<any> => {
    const params = { fromDate, toDate };
    const { data } = await apiClient.get(`/api/merchants/${merchantId}/analytics`, { params });
    return data;
  },
};

// Health API
export const healthApi = {
  check: async (): Promise<any> => {
    const { data } = await apiClient.get('/health');
    return data;
  },
};

export default apiClient;
