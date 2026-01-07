import * as signalR from '@microsoft/signalr';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5153';

export interface NotificationMessage {
  id: string;
  type: string;
  title: string;
  message: string;
  severity: 'success' | 'info' | 'warning' | 'error';
  timestamp: string;
  data?: Record<string, any>;
}

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private isConnecting = false;

  async connect(onNotification: (notification: NotificationMessage) => void): Promise<void> {
    // Prevent multiple simultaneous connection attempts
    if (this.isConnecting) {
      console.log('SignalR connection already in progress');
      return;
    }

    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      console.log('SignalR already connected');
      return;
    }

    this.isConnecting = true;

    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${API_BASE_URL}/hubs/notifications`)
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
              console.error('Max reconnection attempts reached');
              return null;
            }
            return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
          },
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      this.connection.on('notification', (notification: NotificationMessage) => {
        console.log('Received notification:', notification);
        onNotification(notification);
      });

      this.connection.onreconnecting((error) => {
        console.warn('SignalR reconnecting...', error);
        this.reconnectAttempts++;
      });

      this.connection.onreconnected((connectionId) => {
        console.log('SignalR reconnected:', connectionId);
        this.reconnectAttempts = 0;
      });

      this.connection.onclose((error) => {
        console.error('SignalR connection closed', error);
        this.connection = null;
        this.isConnecting = false;
      });

      await this.connection.start();
      console.log('SignalR connected successfully');
      this.reconnectAttempts = 0;
    } catch (error) {
      console.error('Error connecting to SignalR:', error);
      this.connection = null;
      throw error;
    } finally {
      this.isConnecting = false;
    }
  }

  async disconnect(): Promise<void> {
    // Mark as not connecting
    this.isConnecting = false;
    
    if (this.connection) {
      const currentState = this.connection.state;
      
      // Only try to stop if connected or connecting
      if (currentState === signalR.HubConnectionState.Connected || 
          currentState === signalR.HubConnectionState.Connecting) {
        try {
          await this.connection.stop();
          console.log('SignalR disconnected');
        } catch (error) {
          // Ignore errors during disconnect
          console.debug('Error disconnecting SignalR (expected during cleanup):', error);
        }
      }
      
      this.connection = null;
    }
  }

  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}

export const signalRService = new SignalRService();
