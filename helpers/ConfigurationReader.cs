using System.Text.Json;

namespace StockQuoteAlert.Helpers;

/// <summary>
/// Reads email configuration from a JSON file
/// </summary>
public class ConfigurationReader
{
    private readonly string _configPath;

    public ConfigurationReader(string configPath)
    {
        _configPath = configPath ?? throw new ArgumentNullException(nameof(configPath));
    }

    public EmailConfiguration LoadEmailConfiguration()
    {
        if (!File.Exists(_configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {_configPath}");
        }

        var json = File.ReadAllText(_configPath);
        var config = JsonSerializer.Deserialize<EmailConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config == null)
        {
            throw new InvalidOperationException("Failed to deserialize configuration.");
        }

        return config;
    }
}
