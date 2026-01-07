import { AlertTriangle } from 'lucide-react';
import { Button } from './Button';

interface ConfirmDialogProps {
  isOpen: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: 'danger' | 'primary';
  onConfirm: () => void;
  onCancel: () => void;
  isLoading?: boolean;
}

export function ConfirmDialog({
  isOpen,
  title,
  message,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  variant = 'primary',
  onConfirm,
  onCancel,
  isLoading = false,
}: ConfirmDialogProps) {
  if (!isOpen) return null;

  return (
    <div 
      className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50"
      data-testid="confirm-dialog-overlay"
    >
      <div className="bg-white rounded-lg max-w-md w-full p-6" data-testid="confirm-dialog">
        <div className="flex items-start gap-4">
          {variant === 'danger' && (
            <div className="flex-shrink-0 w-10 h-10 rounded-full bg-red-100 flex items-center justify-center">
              <AlertTriangle className="w-5 h-5 text-red-600" />
            </div>
          )}
          <div className="flex-1">
            <h2 className="text-xl font-bold text-gray-900 mb-2" data-testid="confirm-dialog-title">
              {title}
            </h2>
            <p className="text-gray-600 mb-6" data-testid="confirm-dialog-message">
              {message}
            </p>
            <div className="flex gap-3 justify-end">
              <Button
                variant="secondary"
                onClick={onCancel}
                disabled={isLoading}
                data-testid="confirm-dialog-cancel"
              >
                {cancelLabel}
              </Button>
              <Button
                variant={variant}
                onClick={onConfirm}
                disabled={isLoading}
                data-testid="confirm-dialog-confirm"
              >
                {isLoading ? 'Processing...' : confirmLabel}
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
