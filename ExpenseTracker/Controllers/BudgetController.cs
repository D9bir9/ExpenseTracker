using ExpenseTracker.Data.Repository.IRepository;
using ExpenseTracker.Models;
using Microsoft.AspNetCore.Authorization;
using ExpenseTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers
{
    [Authorize]
    public class BudgetController : UserScopedController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrencyConversionService _currencyService;

        public BudgetController(IUnitOfWork unitOfWork, ICurrencyConversionService currencyService)
        {
            _unitOfWork = unitOfWork;
            _currencyService = currencyService;
        }

        public async Task<IActionResult> Index(string sortBy = "over-budget")
        {
            sortBy = NormalizeSortBy(sortBy);
            var overview = await GetBudgetOverviewAsync(sortBy);
            return View(overview);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLimit(int categoryId, decimal monthlyBudgetLimit, string sortBy = "over-budget")
        {
            sortBy = NormalizeSortBy(sortBy);
            var category = _unitOfWork.Category.GetById(
                c => c.CategoryId == categoryId && c.OwnerId == CurrentUserId, "");
            if (category == null || category.Type != "Expense")
                return NotFound();

            if (!ModelState.IsValid || monthlyBudgetLimit < 0)
            {
                TempData["BudgetError"] = "Enter a valid monthly budget limit of zero or more.";
                return RedirectToAction(nameof(Index), new { sortBy });
            }

            var targetCurrency = _currencyService.GetCurrencyCode(
                System.Globalization.CultureInfo.CurrentCulture.Name);
            category.MonthlyBudgetLimit = await _currencyService.ConvertAsync(
                monthlyBudgetLimit, targetCurrency, "USD");
            _unitOfWork.Category.Update(category);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index), new { sortBy });
        }

        private static string NormalizeSortBy(string? sortBy) =>
            sortBy is "highest-spend" or "category" ? sortBy : "over-budget";

        private async Task<BudgetOverview> GetBudgetOverviewAsync(string sortBy)
        {
            var currentCulture = System.Globalization.CultureInfo.CurrentCulture.Name;
            var targetCurrency = _currencyService.GetCurrencyCode(currentCulture);
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
            var previousMonthStart = monthStart.AddMonths(-1);

            var periodTransactions = _unitOfWork.Transaction.GetRange(
                    t => t.OwnerId == CurrentUserId && t.Date >= previousMonthStart && t.Date <= monthEnd,
                    "Category")
                .ToList();
            var convertedAmounts = await ConvertTransactionsAsync(periodTransactions, targetCurrency);

            var categories = _unitOfWork.Category.GetRange(
                    c => c.OwnerId == CurrentUserId && c.Type == "Expense")
                .ToList();
            var convertedLimits = await Task.WhenAll(categories.Select(async category =>
                (category.CategoryId, Limit: await _currencyService.ConvertAsync(
                    category.MonthlyBudgetLimit, "USD", targetCurrency))));
            var limitsByCategory = convertedLimits.ToDictionary(x => x.CategoryId, x => x.Limit);

            var summaries = new List<BudgetCategorySummary>();

            foreach (var category in categories)
            {
                var categoryTransactions = periodTransactions.Where(t => t.CategoryId == category.CategoryId).ToList();
                var spent = categoryTransactions
                    .Where(t => t.Date >= monthStart && t.Date <= monthEnd)
                    .Sum(t => convertedAmounts[t.TransactionId]);
                var lastMonthSpent = categoryTransactions
                    .Where(t => t.Date >= previousMonthStart && t.Date < monthStart)
                    .Sum(t => convertedAmounts[t.TransactionId]);

                var limit = limitsByCategory[category.CategoryId];
                var remaining = limit - spent;
                var progressPercent = limit > 0 ? Math.Min((spent / limit) * 100m, 100m) : 0m;

                var status = limit <= 0
                    ? "No limit set"
                    : spent > limit
                        ? "Over budget"
                        : spent >= limit * 0.8m
                            ? "Near limit"
                            : "On track";

                summaries.Add(new BudgetCategorySummary
                {
                    CategoryId = category.CategoryId,
                    CategoryTitle = category.TitleWithIcon ?? category.Title,
                    Limit = limit,
                    Spent = spent,
                    LastMonthSpent = lastMonthSpent,
                    Remaining = remaining,
                    ProgressPercent = progressPercent,
                    Status = status,
                    StatusClass = limit <= 0
                        ? "bg-secondary"
                        : spent > limit
                            ? "bg-danger"
                            : spent >= limit * 0.8m
                                ? "bg-warning text-dark"
                                : "bg-success"
                });
            }

            summaries = sortBy switch
            {
                "highest-spend" => summaries.OrderByDescending(x => x.Spent).ThenBy(x => x.CategoryTitle).ToList(),
                "category" => summaries.OrderBy(x => x.CategoryTitle).ToList(),
                _ => summaries.OrderByDescending(x => x.Status == "Over budget")
                    .ThenByDescending(x => x.Status == "Near limit")
                    .ThenByDescending(x => x.Spent)
                    .ToList()
            };

            var totalBudget = summaries.Sum(x => x.Limit);
            var totalBudgetedSpent = summaries.Where(x => x.Limit > 0).Sum(x => x.Spent);
            var totalSpent = summaries.Sum(x => x.Spent);
            var totalRemaining = totalBudget - totalBudgetedSpent;
            var totalLastMonthSpent = summaries.Sum(x => x.LastMonthSpent);
            var monthOverMonthChange = totalSpent - totalLastMonthSpent;

            return new BudgetOverview
            {
                MonthLabel = monthStart.ToString("MMM yyyy"),
                TotalBudget = totalBudget,
                TotalSpent = totalSpent,
                TotalBudgetedSpent = totalBudgetedSpent,
                TotalRemaining = totalRemaining,
                TotalRemainingPercent = totalBudget > 0 ? (totalRemaining / totalBudget) * 100m : 0m,
                LastMonthSpent = totalLastMonthSpent,
                MonthOverMonthChange = monthOverMonthChange,
                MonthOverMonthChangePercent = totalLastMonthSpent > 0
                    ? (monthOverMonthChange / totalLastMonthSpent) * 100m
                    : null,
                MonthComparisonData = new List<BudgetPeriodSpend>
                {
                    new() { Period = previousMonthStart.ToString("MMM"), Amount = totalLastMonthSpent },
                    new() { Period = monthStart.ToString("MMM"), Amount = totalSpent }
                },
                SortBy = sortBy,
                Categories = summaries
            };
        }

        private async Task<Dictionary<int, decimal>> ConvertTransactionsAsync(
            IReadOnlyCollection<Transaction> transactions,
            string targetCurrency)
        {
            var currencies = transactions
                .Select(t => _currencyService.GetCurrencyCode(t.CultureCode ?? "en-US"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(currency => !string.Equals(currency, targetCurrency, StringComparison.OrdinalIgnoreCase))
                .ToList();

            await Task.WhenAll(currencies.Select(currency =>
                _currencyService.ConvertAsync(1m, currency, targetCurrency)));

            var amounts = await Task.WhenAll(transactions.Select(async transaction =>
            {
                var sourceCurrency = _currencyService.GetCurrencyCode(transaction.CultureCode ?? "en-US");
                var amount = await _currencyService.ConvertAsync(transaction.Amount, sourceCurrency, targetCurrency);
                return (transaction.TransactionId, amount);
            }));

            return amounts.ToDictionary(x => x.TransactionId, x => x.amount);
        }
    }

    public class BudgetOverview
    {
        public string MonthLabel { get; set; } = string.Empty;
        public decimal TotalBudget { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal TotalBudgetedSpent { get; set; }
        public decimal TotalRemaining { get; set; }
        public decimal TotalRemainingPercent { get; set; }
        public decimal LastMonthSpent { get; set; }
        public decimal MonthOverMonthChange { get; set; }
        public decimal? MonthOverMonthChangePercent { get; set; }
        public List<BudgetPeriodSpend> MonthComparisonData { get; set; } = new();
        public string SortBy { get; set; } = "over-budget";
        public List<BudgetCategorySummary> Categories { get; set; } = new();
    }

    public class BudgetPeriodSpend
    {
        public string Period { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class BudgetCategorySummary
    {
        public int CategoryId { get; set; }
        public string CategoryTitle { get; set; } = string.Empty;
        public decimal Limit { get; set; }
        public decimal Spent { get; set; }
        public decimal LastMonthSpent { get; set; }
        public decimal Remaining { get; set; }
        public decimal ProgressPercent { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
    }
}
