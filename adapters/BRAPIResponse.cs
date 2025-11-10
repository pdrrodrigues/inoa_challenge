namespace StockQuoteAlert.Adapters;

public sealed class BRAPIResponse
{
    public IReadOnlyList<BRAPIResult> Results { get; init; } = Array.Empty<BRAPIResult>();

    public DateTimeOffset RequestedAt { get; init; }

    public string Took { get; init; } = string.Empty;
}

public sealed class BRAPIResult
{
    public string Currency { get; init; } = string.Empty;

    public long MarketCap { get; init; }

    public string ShortName { get; init; } = string.Empty;

    public string LongName { get; init; } = string.Empty;

    public decimal RegularMarketChange { get; init; }

    public decimal RegularMarketChangePercent { get; init; }

    public DateTimeOffset RegularMarketTime { get; init; }

    public decimal RegularMarketPrice { get; init; }

    public decimal RegularMarketDayHigh { get; init; }

    public string RegularMarketDayRange { get; init; } = string.Empty;

    public decimal RegularMarketDayLow { get; init; }

    public long RegularMarketVolume { get; init; }

    public decimal RegularMarketPreviousClose { get; init; }

    public decimal RegularMarketOpen { get; init; }

    public string FiftyTwoWeekRange { get; init; } = string.Empty;

    public decimal FiftyTwoWeekLow { get; init; }

    public decimal FiftyTwoWeekHigh { get; init; }

    public string Symbol { get; init; } = string.Empty;

    public string LogoUrl { get; init; } = string.Empty;

    public decimal PriceEarnings { get; init; }

    public decimal EarningsPerShare { get; init; }
}
