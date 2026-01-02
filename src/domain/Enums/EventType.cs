namespace FintechPlatform.Domain.Enums;

/// <summary>
/// Types of domain events in the system
/// </summary>
public enum EventType
{
    /// <summary>
    /// Payment has been created with Pending status
    /// </summary>
    PaymentCreated,
    
    /// <summary>
    /// Payment has been completed with funds credited
    /// </summary>
    PaymentCompleted,
    
    /// <summary>
    /// Withdrawal has been requested
    /// </summary>
    WithdrawalRequested,
    
    /// <summary>
    /// Withdrawal has been processed
    /// </summary>
    WithdrawalProcessed
}
