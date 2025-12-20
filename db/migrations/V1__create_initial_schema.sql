-- Create merchants table
CREATE TABLE merchants (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT merchants_email_unique UNIQUE (email)
);

CREATE INDEX idx_merchants_email ON merchants(email);
CREATE INDEX idx_merchants_is_active ON merchants(is_active);

-- Create payments table
CREATE TABLE payments (
    id UUID PRIMARY KEY,
    merchant_id UUID NOT NULL,
    amount_in_minor_units BIGINT NOT NULL CHECK (amount_in_minor_units > 0),
    currency VARCHAR(3) NOT NULL,
    status INTEGER NOT NULL,
    external_reference VARCHAR(255),
    description VARCHAR(1000),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    completed_at TIMESTAMP WITH TIME ZONE,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT fk_payments_merchant FOREIGN KEY (merchant_id) REFERENCES merchants(id)
);

CREATE INDEX idx_payments_merchant_id ON payments(merchant_id);
CREATE INDEX idx_payments_status ON payments(status);
CREATE INDEX idx_payments_external_reference ON payments(external_reference);
CREATE INDEX idx_payments_created_at ON payments(created_at);

-- Create balances table
CREATE TABLE balances (
    id UUID PRIMARY KEY,
    merchant_id UUID NOT NULL,
    available_balance_in_minor_units BIGINT NOT NULL DEFAULT 0 CHECK (available_balance_in_minor_units >= 0),
    pending_balance_in_minor_units BIGINT NOT NULL DEFAULT 0 CHECK (pending_balance_in_minor_units >= 0),
    currency VARCHAR(3) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL,
    version INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT fk_balances_merchant FOREIGN KEY (merchant_id) REFERENCES merchants(id),
    CONSTRAINT balances_merchant_currency_unique UNIQUE (merchant_id, currency)
);

CREATE INDEX idx_balances_merchant_id ON balances(merchant_id);

-- Create withdrawals table
CREATE TABLE withdrawals (
    id UUID PRIMARY KEY,
    merchant_id UUID NOT NULL,
    amount_in_minor_units BIGINT NOT NULL CHECK (amount_in_minor_units > 0),
    currency VARCHAR(3) NOT NULL,
    status INTEGER NOT NULL,
    bank_account_number VARCHAR(50) NOT NULL,
    bank_routing_number VARCHAR(50) NOT NULL,
    external_transaction_id VARCHAR(255),
    failure_reason VARCHAR(1000),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    processed_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT fk_withdrawals_merchant FOREIGN KEY (merchant_id) REFERENCES merchants(id)
);

CREATE INDEX idx_withdrawals_merchant_id ON withdrawals(merchant_id);
CREATE INDEX idx_withdrawals_status ON withdrawals(status);
CREATE INDEX idx_withdrawals_created_at ON withdrawals(created_at);

-- Create ledger_entries table
CREATE TABLE ledger_entries (
    id UUID PRIMARY KEY,
    merchant_id UUID NOT NULL,
    entry_type INTEGER NOT NULL,
    amount_in_minor_units BIGINT NOT NULL,
    currency VARCHAR(3) NOT NULL,
    balance_after_in_minor_units BIGINT NOT NULL CHECK (balance_after_in_minor_units >= 0),
    related_payment_id UUID,
    related_withdrawal_id UUID,
    description VARCHAR(1000),
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT fk_ledger_entries_merchant FOREIGN KEY (merchant_id) REFERENCES merchants(id),
    CONSTRAINT fk_ledger_entries_payment FOREIGN KEY (related_payment_id) REFERENCES payments(id),
    CONSTRAINT fk_ledger_entries_withdrawal FOREIGN KEY (related_withdrawal_id) REFERENCES withdrawals(id)
);

CREATE INDEX idx_ledger_entries_merchant_id ON ledger_entries(merchant_id);
CREATE INDEX idx_ledger_entries_related_payment_id ON ledger_entries(related_payment_id);
CREATE INDEX idx_ledger_entries_related_withdrawal_id ON ledger_entries(related_withdrawal_id);
CREATE INDEX idx_ledger_entries_created_at ON ledger_entries(created_at DESC);
CREATE INDEX idx_ledger_entries_merchant_currency ON ledger_entries(merchant_id, currency);

COMMENT ON TABLE merchants IS 'Stores merchant account information';
COMMENT ON TABLE payments IS 'Stores all payment transactions';
COMMENT ON TABLE balances IS 'Stores merchant balances with optimistic locking';
COMMENT ON TABLE withdrawals IS 'Stores withdrawal requests and their status';
COMMENT ON TABLE ledger_entries IS 'Immutable audit log of all financial transactions';

COMMENT ON COLUMN balances.version IS 'Optimistic concurrency control version';
COMMENT ON COLUMN ledger_entries.amount_in_minor_units IS 'Positive for credit, negative for debit';
