-- Add CompletedBy column to payments table to track completion source
ALTER TABLE payments
ADD COLUMN completed_by INTEGER;

COMMENT ON COLUMN payments.completed_by IS 'Indicates who/what completed the payment: 0=Manual, 1=FraudDetection';
