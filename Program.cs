using StockQuoteAlert.Adapters;
using StockQuoteAlert.Helpers;
using StockQuoteAlert.ServiceLayer;

internal class Program
{
    private static string BRAPI_KEY = "YOUR_BRAPI_KEY";

    private static async Task<int> Main(string[] args)
    {
        var parseResult = CliParser.Parse(args);

        if (parseResult.IsHelpRequest)
        {
            PrintHelp();
            return 0;
        }

        if (!parseResult.IsSuccess || parseResult.Arguments is null)
        {
            Console.Error.WriteLine(parseResult.ErrorMessage);
            PrintHelp();
            return 1;
        }

        var cliArgs = parseResult.Arguments;

        Console.WriteLine($"Preparing alert for {cliArgs.StockTicker}.");
        Console.WriteLine($"  Buy below : {cliArgs.BuyPrice}");
        Console.WriteLine($"  Sell above: {cliArgs.SellPrice}");
        Console.WriteLine($"  Provider  : {cliArgs.Provider}");

        if (string.IsNullOrWhiteSpace(cliArgs.ConfigPath))
        {
            Console.Error.WriteLine("Error: Config path is required.");
            return 1;
        }

        Console.WriteLine($"  Config    : {cliArgs.ConfigPath}");

        try
        {
            var configReader = new ConfigurationReader(cliArgs.ConfigPath);
            var emailConfig = configReader.LoadEmailConfiguration();
            var notificationSender = new MailKitEmailSender(emailConfig);
            var httpClient = new HttpClient();
            var priceProvider = new BRAPIPriceProvider(BRAPI_KEY, httpClient);

            using var service = new StockPriceAlertService(
                priceProvider: priceProvider,
                notificationSender: notificationSender,
                stockTicker: cliArgs.StockTicker,
                buyPrice: cliArgs.BuyPrice,
                sellPrice: cliArgs.SellPrice,
                pollingInterval: TimeSpan.FromMinutes(5)
            );

            service.Start();

            Console.WriteLine("Press Ctrl+C to stop...");
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }

        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  stock-quote-alert STOCK_TICK SELL_PRICE BUY_PRICE [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -c, --config <path>    Path to configuration file (required).");
        // Console.WriteLine("  -p, --provider <name>  API provider name.");
        Console.WriteLine("  -h, --help             Show this message.");
    }
}
