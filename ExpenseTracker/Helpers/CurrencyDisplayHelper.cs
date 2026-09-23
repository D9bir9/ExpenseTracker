// Helpers/CurrencyDisplayHelper.cs
using System.Globalization;

namespace ExpenseTracker.Helpers
{
    public static class CurrencyDisplayHelper
    {
        // Font Awesome only ships icons for a handful of currencies.
        // Everything else falls back to a generic icon.
        private static readonly Dictionary<string, string> CurrencyIconMap = new()
        {
            { "USD", "fa-dollar-sign" },
            { "CAD", "fa-dollar-sign" },
            { "AUD", "fa-dollar-sign" },
            { "EUR", "fa-euro-sign" },
            { "GBP", "fa-pound-sign" },
            { "JPY", "fa-yen-sign" },
            { "CNY", "fa-yen-sign" },
            { "INR", "fa-rupee-sign" },
            { "RUB", "fa-ruble-sign" },
            { "KRW", "fa-won-sign" },
            { "ILS", "fa-shekel-sign" },
            { "TRY", "fa-lira-sign" },
            { "NGN", "fa-naira-sign" },
        };

        public static string GetIconClass(CultureInfo? culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;

            try
            {
                var region = new RegionInfo(culture.Name);
                if (CurrencyIconMap.TryGetValue(region.ISOCurrencySymbol, out var icon))
                    return icon;
            }
            catch (ArgumentException)
            {
                // Invariant culture or something without a region — fall through
            }

            return "fa-money-bill-wave"; // sensible generic fallback
        }
    }
}