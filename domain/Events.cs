namespace StockQuoteAlert.Domain;

/// <summary>
/// Base interface for all domain events
/// </summary>
public interface IEvent
{
    DateTime Timestamp { get; }
}

/// <summary>
/// Event triggered when stock price crosses the buy threshold
/// </summary>
public record PriceBelowThresholdEvent(string StockTicker, decimal CurrentPrice, decimal Threshold, DateTime Timestamp) : IEvent;

/// <summary>
/// Event triggered when stock price crosses the sell threshold
/// </summary>
public record PriceAboveThresholdEvent(string StockTicker, decimal CurrentPrice, decimal Threshold, DateTime Timestamp) : IEvent;

/// <summary>
/// Event triggered when price check fails
/// </summary>
public record PriceCheckFailedEvent(string StockTicker, Exception Exception, DateTime Timestamp) : IEvent;
