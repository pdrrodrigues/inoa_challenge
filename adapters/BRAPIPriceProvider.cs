using System.Text.Json;
using System.Net.Http.Headers;

namespace StockQuoteAlert.Adapters;

public class BRAPIPriceProvider : IPriceProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public BRAPIPriceProvider(string apiKey, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    }

    public async Task<decimal> GetCurrentPriceAsync(string stockTicker, CancellationToken cancellationToken = default)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://brapi.dev/api/quote/{stockTicker}");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var quote = JsonSerializer.Deserialize<BRAPIResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return quote?.Results?.FirstOrDefault()?.RegularMarketPrice ?? throw new InvalidOperationException("Invalid response from BRAPI");
    }
}
