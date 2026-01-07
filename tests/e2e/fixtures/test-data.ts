export const testMerchants = {
  merchant1: {
    name: 'Test Corp',
    email: 'test@corp.com'
  },
  merchant2: {
    name: 'Acme Inc',
    email: 'acme@inc.com'
  }
};

export const testPayments = {
  small: { amount: '10.00', currency: 'USD' },
  medium: { amount: '100.00', currency: 'USD' },
  large: { amount: '1000.00', currency: 'USD' }
};

export function generateUniqueMerchant() {
  const timestamp = Date.now();
  return {
    name: `Test Merchant ${timestamp}`,
    email: `test${timestamp}@example.com`
  };
}
