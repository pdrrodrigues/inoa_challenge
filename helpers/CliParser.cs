using System.Globalization;

namespace StockQuoteAlert.Helpers;

internal static class CliParser
{
    private const string DEFAULT_CONFIG_PATH = "config.json";
    private const string DEFAULT_PROVIDER = "brapi";

    public static CliParseResult Parse(string[] args)
    {
        var requiredValues = new List<string>();
        string? configPath = null;
        string provider = DEFAULT_PROVIDER;
        bool helpRequested = false;

        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];

            switch (current)
            {
                case "--help":
                case "-h":
                    helpRequested = true;
                    break;
                case "--config":
                case "-c":
                    if (!TryReadNextValue(args, ref i, out configPath))
                    {
                        return CliParseResult.Failure("Missing value after --config option.");
                    }

                    break;
                // case "--provider":
                // case "-p":
                //     if (!TryReadNextValue(args, ref i, out var providerValue))
                //     {
                //         return CliParseResult.Failure("Missing value after --provider option.");
                //     }

                //     provider = providerValue!;
                //     break;
                default:
                    if (current.StartsWith('-'))
                    {
                        return CliParseResult.Failure($"Unknown option '{current}'.");
                    }

                    requiredValues.Add(current);
                    break;
            }
        }

        if (helpRequested)
        {
            return CliParseResult.Help();
        }

        if (requiredValues.Count != 3)
        {
            return CliParseResult.Failure("Expecting STOCK_TICK, SELL_PRICE, and BUY_PRICE arguments.");
        }

        var stockTicker = requiredValues[0];
        if (string.IsNullOrWhiteSpace(stockTicker))
        {
            return CliParseResult.Failure("STOCK_TICK cannot be empty.");
        }

        if (!TryParseDecimal(requiredValues[1], out var sellPrice))
        {
            return CliParseResult.Failure("SELL_PRICE must be a decimal number.");
        }

        if (!TryParseDecimal(requiredValues[2], out var buyPrice))
        {
            return CliParseResult.Failure("BUY_PRICE must be a decimal number.");
        }

        configPath = configPath ?? DEFAULT_CONFIG_PATH;
        var argsResult = new CliArguments(buyPrice, sellPrice, stockTicker, configPath, provider);
        return CliParseResult.Success(argsResult);
    }

    private static bool TryReadNextValue(IReadOnlyList<string> args, ref int index, out string? value)
    {
        if (index + 1 >= args.Count)
        {
            value = null;
            return false;
        }

        value = args[index + 1];
        index++;
        return true;
    }

    private static bool TryParseDecimal(string value, out decimal parsed)
    {
        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
    }
}

internal sealed record CliArguments(
    decimal BuyPrice,
    decimal SellPrice,
    string StockTicker,
    string ConfigPath,
    string Provider);

internal sealed record CliParseResult(
    bool IsSuccess,
    bool IsHelpRequest,
    CliArguments? Arguments,
    string? ErrorMessage)
{
    public static CliParseResult Success(CliArguments args) => new(true, false, args, null);

    public static CliParseResult Failure(string message) =>
        new(false, false, null, message);

    public static CliParseResult Help() =>
        new(false, true, null, null);
}
