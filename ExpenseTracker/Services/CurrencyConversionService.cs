
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace ExpenseTracker.Services
{
    public class CurrencyConversionService : ICurrencyConversionService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);
 
        public CurrencyConversionService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }
 
        public string GetCurrencyCode(string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
                return "USD";

            var allowedCurrencies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["en-US"] = "USD",
                ["en-GB"] = "GBP",
                ["en-NG"] = "NGN"
            };

            if (allowedCurrencies.TryGetValue(cultureName.Trim(), out var allowedCurrency))
                return allowedCurrency;

            try
            {
                var region = new RegionInfo(cultureName);
                var isoCurrency = region.ISOCurrencySymbol;
                return isoCurrency is "USD" or "GBP" or "NGN" ? isoCurrency : "USD";
            }
            catch (ArgumentException)
            {
                return "USD";
            }
        }
 
        public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
        {
            if (amount == 0m)
                return 0m;

            if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
                return amount;
 
            var rate = await GetExchangeRateAsync(fromCurrency, toCurrency);
            return Math.Round(amount * rate, 2);
        }
 
        private async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            string cacheKey = $"fxrate:{fromCurrency}:{toCurrency}";

            if (_cache.TryGetValue(cacheKey, out decimal cachedRate))
                return cachedRate;

            // open.er-api.com supports NGN and the currencies used in this budget app.
            var url = $"https://open.er-api.com/v6/latest/{fromCurrency}";

            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (doc.RootElement.TryGetProperty("result", out var result) &&
                result.GetString() == "error")
            {
                throw new InvalidOperationException("Exchange rate service returned an error.");
            }

            if (!doc.RootElement.TryGetProperty("rates", out var rates) ||
                !rates.TryGetProperty(toCurrency, out var rateElement))
            {
                throw new InvalidOperationException($"Exchange rate unavailable for {fromCurrency} -> {toCurrency}.");
            }

            decimal rate = rateElement.GetDecimal();

            _cache.Set(cacheKey, rate, CacheDuration);
            return rate;
        }
    }

}