-- Add refund tracking to payments
ALTER TABLE payments 
ADD COLUMN refunded_at TIMESTAMP,
ADD COLUMN refund_reason TEXT,
ADD COLUMN refunded_amount_in_minor_units BIGINT;

-- Add index for refunded payments
CREATE INDEX idx_payments_refunded_at ON payments(refunded_at) WHERE refunded_at IS NOT NULL;
