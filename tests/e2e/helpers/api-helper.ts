import { APIRequestContext } from '@playwright/test';

const API_BASE_URL = process.env.API_URL || 'http://localhost:5153';

export class ApiHelper {
  constructor(private request: APIRequestContext) {}

  async createMerchant(name: string, email: string) {
    const response = await this.request.post(`${API_BASE_URL}/api/merchants`, {
      data: { name, email }
    });
    return await response.json();
  }

  async getMerchant(id: string) {
    const response = await this.request.get(`${API_BASE_URL}/api/merchants/${id}`);
    return await response.json();
  }

  async createPayment(merchantId: string, amountInMinorUnits: number, currency: string = 'USD') {
    const response = await this.request.post(`${API_BASE_URL}/api/merchants/${merchantId}/payments`, {
      data: {
        amountInMinorUnits,
        currency,
        description: 'E2E Test Payment'
      }
    });
    return await response.json();
  }

  async getBalance(merchantId: string, currency: string = 'USD') {
    const response = await this.request.get(`${API_BASE_URL}/api/merchants/${merchantId}/balances/${currency}`);
    return await response.json();
  }
}
