namespace StockQuoteAlert.Adapters;

/// <summary>
/// Interface for stock price providers
/// </summary>
public interface IPriceProvider
{
    Task<decimal> GetCurrentPriceAsync(string stockTicker, CancellationToken cancellationToken = default);
}
