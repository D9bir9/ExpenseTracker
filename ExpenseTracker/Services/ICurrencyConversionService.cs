using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpenseTracker.Services
{
    public interface ICurrencyConversionService
    {
        /// <summary>
        /// Converts an amount from one ISO currency code to another (e.g. "USD" -> "GBP").
        /// Returns the amount unchanged if the codes match.
        /// </summary>
        Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);

        /// <summary>
        /// Maps a culture name (e.g. "en-US", "en-GB") to its ISO currency code (e.g. "USD", "GBP").
        /// </summary>
        string GetCurrencyCode(string cultureName);

    }
}