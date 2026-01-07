import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider, useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';
import { Navigation } from './components/Navigation';
import { ScrollToTop } from './components/ScrollToTop';
import { ToastContainer } from './components/Toast';
import { useToast } from './hooks/useToast';
import { signalRService } from './services/signalRService';
import { Dashboard } from './pages/Dashboard';
import { Merchants } from './pages/Merchants';
import { MerchantDetail } from './pages/MerchantDetail';
import { Analytics } from './pages/Analytics';
import { SystemHealth } from './pages/SystemHealth';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
      staleTime: 30000, // 30 seconds
    },
  },
});

function AppContent() {
  const { toasts, removeToast, addToast } = useToast();
  const queryClient = useQueryClient();

  useEffect(() => {
    let isCleaningUp = false;

    // Connect to SignalR hub for real-time notifications
    const connectSignalR = async () => {
      // Small delay to ensure component is stable
      await new Promise(resolve => setTimeout(resolve, 100));
      
      // Don't connect if we're already cleaning up
      if (isCleaningUp) return;

      try {
        await signalRService.connect((notification) => {
          console.log('Received notification:', notification);
          
          // Show toast notification
          addToast(
            notification.message,
            notification.severity as 'success' | 'error' | 'warning',
            5000
          );
          
          // Invalidate queries to refresh data
          if (notification.type === 'PaymentCompleted') {
            queryClient.invalidateQueries({ queryKey: ['payments'] });
            queryClient.invalidateQueries({ queryKey: ['balances'] });
            queryClient.invalidateQueries({ queryKey: ['dashboard'] });
          }
        });
      } catch (error) {
        // Only show error if we're not cleaning up
        if (!isCleaningUp) {
          console.error('Failed to connect to notification hub:', error);
        }
      }
    };

    connectSignalR();

    // Cleanup on unmount
    return () => {
      isCleaningUp = true;
      // Small delay before disconnect to avoid race condition
      setTimeout(() => {
        signalRService.disconnect();
      }, 50);
    };
  }, []); // Empty dependency array - only run once

  return (
    <div className="min-h-screen bg-gray-50">
      <ScrollToTop />
      <Navigation />
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/merchants" element={<Merchants />} />
          <Route path="/merchants/:id" element={<MerchantDetail />} />
          <Route path="/merchants/:id/analytics" element={<Analytics />} />
          <Route path="/health" element={<SystemHealth />} />
        </Routes>
      </main>
      <ToastContainer toasts={toasts} onClose={removeToast} />
    </div>
  );
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AppContent />
      </BrowserRouter>
    </QueryClientProvider>
  );
}

export default App;
