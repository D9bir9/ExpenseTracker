
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
 
            try
            {
                var region = new RegionInfo(cultureName);
                return region.ISOCurrencySymbol; // e.g. "USD", "GBP", "EUR"
            }
            catch (ArgumentException)
            {
                // Unknown/neutral culture name (e.g. "en") — fall back to a sensible default.
                return "USD";
            }
        }
 
        public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
        {
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
 
            // Frankfurter (https://frankfurter.dev) is a free, no-API-key exchange rate
            // service backed by European Central Bank reference rates.
            // Swap this out for a paid provider if you need more currencies or intraday rates.
            var url = $"https://api.frankfurter.app/latest?from={fromCurrency}&to={toCurrency}";
 
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
 
            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
 
            decimal rate = doc.RootElement
                .GetProperty("rates")
                .GetProperty(toCurrency)
                .GetDecimal();
 
            _cache.Set(cacheKey, rate, CacheDuration);
            return rate;
        }
    }

}