using StockQuoteAlert.Adapters;
using StockQuoteAlert.Domain;

namespace StockQuoteAlert.ServiceLayer;

/// <summary>
/// Main service class that polls stock prices and sends alerts
/// </summary>
public class StockPriceAlertService : IDisposable
{
    private readonly IPriceProvider _priceProvider;
    private readonly INotificationSender _notificationSender;
    private readonly IEventBus _eventBus;
    private readonly string _stockTicker;
    private readonly decimal _buyPrice;
    private readonly decimal _sellPrice;
    private readonly TimeSpan _pollingInterval;

    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _pollingTask;

    public StockPriceAlertService(
        IPriceProvider priceProvider,
        INotificationSender notificationSender,
        string stockTicker,
        decimal buyPrice,
        decimal sellPrice,
        TimeSpan? pollingInterval = null,
        IEventBus? eventBus = null)
    {
        _priceProvider = priceProvider ?? throw new ArgumentNullException(nameof(priceProvider));
        _notificationSender = notificationSender ?? throw new ArgumentNullException(nameof(notificationSender));
        _stockTicker = stockTicker ?? throw new ArgumentNullException(nameof(stockTicker));
        _buyPrice = buyPrice;
        _sellPrice = sellPrice;
        _pollingInterval = pollingInterval ?? TimeSpan.FromMinutes(1);

        // Initialize event bus and subscribe to events
        _eventBus = eventBus ?? new InMemoryEventBus();
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        // Subscribe to price below threshold event
        _eventBus.Subscribe<PriceBelowThresholdEvent>(async @event =>
        {
            Console.WriteLine($"[{@event.Timestamp:yyyy-MM-dd HH:mm:ss}] BUY ALERT: {_stockTicker} is at {@event.CurrentPrice:C}, below threshold of {@event.Threshold:C}");

            await SendBuyAlertAsync(@event);

        });

        // Subscribe to price above threshold event
        _eventBus.Subscribe<PriceAboveThresholdEvent>(async @event =>
        {
            Console.WriteLine($"[{@event.Timestamp:yyyy-MM-dd HH:mm:ss}] SELL ALERT: {_stockTicker} is at {@event.CurrentPrice:C}, above threshold of {@event.Threshold:C}");

            await SendSellAlertAsync(@event);
        });

        // Subscribe to price check failed event
        _eventBus.Subscribe<PriceCheckFailedEvent>(async @event =>
        {
            Console.Error.WriteLine($"[{@event.Timestamp:yyyy-MM-dd HH:mm:ss}] ERROR: Failed to check price for {_stockTicker}: {@event.Exception.Message}");
            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// Start polling for stock prices
    /// </summary>
    public void Start()
    {
        if (_pollingTask != null)
        {
            throw new InvalidOperationException("Service is already running.");
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _pollingTask = Task.Run(() => PollPricesAsync(_cancellationTokenSource.Token));

        Console.WriteLine($"Started monitoring {_stockTicker}. Buy threshold: {_buyPrice:C}, Sell threshold: {_sellPrice:C}");
    }

    private async Task PollPricesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var currentPrice = await _priceProvider.GetCurrentPriceAsync(_stockTicker, cancellationToken);

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Current price for {_stockTicker}: {currentPrice:C}");

                // Check if price is below buy threshold
                if (currentPrice <= _buyPrice)
                {
                    await _eventBus.PublishAsync(new PriceBelowThresholdEvent(_stockTicker, currentPrice, _buyPrice, DateTime.Now));
                }

                // Check if price is above sell threshold
                if (currentPrice >= _sellPrice)
                {
                    await _eventBus.PublishAsync(new PriceAboveThresholdEvent(_stockTicker, currentPrice, _sellPrice, DateTime.Now));
                }
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new PriceCheckFailedEvent(_stockTicker, ex, DateTime.Now));
            }

            await Task.Delay(_pollingInterval, cancellationToken);
        }
    }

    private async Task SendBuyAlertAsync(PriceBelowThresholdEvent @event)
    {
        var subject = $"BUY ALERT: {@event.StockTicker}";
        var body = $"The stock {@event.StockTicker} has reached a buy price!\n\n" +
                   $"Current Price: {@event.CurrentPrice:C}\n" +
                   $"Your Buy Threshold: {@event.Threshold:C}\n" +
                   $"Time: {@event.Timestamp:yyyy-MM-dd HH:mm:ss}\n\n" +
                   $"Consider buying this stock.";

        var message = new NotificationMessage(subject, body, NotificationCategory.BuyAlert);
        await _notificationSender.SendAsync(message);
    }

    private async Task SendSellAlertAsync(PriceAboveThresholdEvent @event)
    {
        var subject = $"SELL ALERT: {@event.StockTicker}";
        var body = $"The stock {@event.StockTicker} has reached a sell price!\n\n" +
                   $"Current Price: {@event.CurrentPrice:C}\n" +
                   $"Your Sell Threshold: {@event.Threshold:C}\n" +
                   $"Time: {@event.Timestamp:yyyy-MM-dd HH:mm:ss}\n\n" +
                   $"Consider selling this stock.";

        var message = new NotificationMessage(subject, body, NotificationCategory.SellAlert);
        await _notificationSender.SendAsync(message);
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();

        if (_notificationSender is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
