using ExpenseTracker.Data.Repository.IRepository;
using ExpenseTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExpenseTracker.Models;

namespace ExpenseTracker.Controllers
{
    [Authorize]
    public class DashboardController : UserScopedController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrencyConversionService _currencyService;

        public DashboardController(IUnitOfWork unitOfWork, ICurrencyConversionService currencyService)
        {
            _unitOfWork = unitOfWork;
            _currencyService = currencyService;
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> GetDashboardData(string dateRange = "7days")
        {
            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;

            string targetCurrency = _currencyService.GetCurrencyCode(System.Globalization.CultureInfo.CurrentCulture.Name);

            int rangeEnd = 7;

            switch (dateRange?.ToLower())
            {
                case "14days":
                    StartDate = DateTime.Today.AddDays(-13);
                    rangeEnd = 14;
                    EndDate = StartDate.AddDays(rangeEnd - 1);
                    break;
                case "30days":
                    StartDate = DateTime.Today.AddDays(-29);
                    rangeEnd = 30;
                    EndDate = StartDate.AddDays(rangeEnd - 1);
                    break;
                case "thismonth":
                    StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    EndDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
                    rangeEnd = EndDate.Day;
                    break;
                default:
                    EndDate = DateTime.Today;
                    break;
            }

            List<Transaction> selectedTransactions = _unitOfWork.Transaction
                .GetRange(y => y.OwnerId == CurrentUserId && y.Date >= StartDate && y.Date <= EndDate, "Category")
                .ToList();

            DateTime monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime previousMonthStart = monthStart.AddMonths(-1);
            DateTime monthEnd = monthStart.AddMonths(1).AddTicks(-1);
            var monthTransactions = _unitOfWork.Transaction.GetRange(
                    t => t.OwnerId == CurrentUserId && t.Date >= previousMonthStart && t.Date <= monthEnd,
                    "Category")
                .ToList();

            // Convert every transaction's amount into targetCurrency BEFORE any
            // aggregation happens. Each transaction's original currency comes from
            // the CultureCode that was stamped on it when it was created (see
            // Transaction model + Create action changes noted separately).
            var convertedAmounts = await ConvertTransactionsAsync(
                selectedTransactions.Concat(monthTransactions).DistinctBy(t => t.TransactionId).ToList(),
                targetCurrency);

            decimal totalIncome = selectedTransactions
                .Where(t => t.Category.Type == "Income")
                .Sum(t => convertedAmounts[t.TransactionId]);

            decimal totalExpense = selectedTransactions
                .Where(t => t.Category.Type == "Expense")
                .Sum(t => convertedAmounts[t.TransactionId]);

            decimal balance = totalIncome - totalExpense;

            // doughnut chart
            var expenseChartData = selectedTransactions
                .Where(t => t.Category.Type == "Expense")
                .GroupBy(t => t.Category.CategoryId)
                .Select(k => new {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => convertedAmounts[j.TransactionId]),
                    formattedAmount = k.Sum(j => convertedAmounts[j.TransactionId]).ToString("c0")
                })
                .OrderBy(t => t.amount)
                .ToList();

            // spline chart Income vs Expense
            // Income
            List<SplineChartData> IncomeSummary = selectedTransactions
                .Where(t => t.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => convertedAmounts[l.TransactionId]),
                }).ToList();

            // Expense
            List<SplineChartData> ExpenseSummary = selectedTransactions
                .Where(t => t.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(l => convertedAmounts[l.TransactionId]),
                }).ToList();

            // Combine Income and Expense by date
            string[] Dates = Enumerable.Range(0, rangeEnd)
                .Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            var SplineChartData = from day in Dates
                join income in IncomeSummary on day equals income.day into dayIncomeJoined
                from income in dayIncomeJoined.DefaultIfEmpty()
                join expense in ExpenseSummary on day equals expense.day into expenseJoined
                from expense in expenseJoined.DefaultIfEmpty()
                select (new
                {
                    day = day,
                    income = income == null ? 0 : income.income,
                    expense = expense == null ? 0 : expense.expense
                });

            // Budget tracking for the current month.
            var categories = _unitOfWork.Category.GetRange(
                    c => c.OwnerId == CurrentUserId && c.Type == "Expense")
                .ToList();
            var convertedLimits = await Task.WhenAll(categories.Select(async category =>
                (category.CategoryId, Limit: await _currencyService.ConvertAsync(
                    category.MonthlyBudgetLimit, "USD", targetCurrency))));
            var limitsByCategory = convertedLimits.ToDictionary(x => x.CategoryId, x => x.Limit);

            var budgetData = new List<DashboardBudgetData>();
            foreach (var category in categories)
            {
                var categoryTransactions = monthTransactions.Where(t => t.CategoryId == category.CategoryId).ToList();
                var spent = categoryTransactions
                    .Where(t => t.Date >= monthStart && t.Date <= monthEnd)
                    .Sum(t => convertedAmounts[t.TransactionId]);
                var lastMonthSpent = categoryTransactions
                    .Where(t => t.Date >= previousMonthStart && t.Date < monthStart)
                    .Sum(t => convertedAmounts[t.TransactionId]);
                var limit = limitsByCategory[category.CategoryId];
                var remaining = limit - spent;
                var progressPercent = limit > 0
                    ? Math.Min((spent / limit) * 100m, 100m)
                    : 0m;

                budgetData.Add(new DashboardBudgetData
                {
                    CategoryTitle = category.TitleWithIcon ?? category.Title,
                    Limit = limit,
                    Spent = spent,
                    LastMonthSpent = lastMonthSpent,
                    Remaining = remaining,
                    HasLimit = limit > 0,
                    OverBudget = limit > 0 && spent > limit,
                    NearLimit = limit > 0 && spent <= limit && spent >= limit * 0.8m,
                    ProgressPercent = progressPercent,
                    LimitText = limit.ToString("C0"),
                    SpentText = spent.ToString("C0"),
                    LastMonthSpentText = lastMonthSpent.ToString("C0"),
                    RemainingText = remaining.ToString("C0")
                });
            }

            budgetData = budgetData
                .Where(x => x.Limit > 0 || x.Spent > 0 || x.LastMonthSpent > 0)
                .OrderByDescending(x => x.OverBudget)
                .ThenByDescending(x => x.NearLimit)
                .ThenByDescending(x => x.Spent)
                .ToList();

            decimal totalBudget = budgetData.Sum(x => x.Limit);
            decimal totalBudgetSpent = budgetData.Where(x => x.Limit > 0).Sum(x => x.Spent);
            decimal totalExpenseSpent = budgetData.Sum(x => x.Spent);
            decimal totalBudgetRemaining = totalBudget - totalBudgetSpent;
            decimal totalBudgetRemainingPercent = totalBudget > 0 ? (totalBudgetRemaining / totalBudget) * 100m : 0m;
            decimal lastMonthTotalSpent = budgetData.Sum(x => x.LastMonthSpent);
            decimal monthOverMonthChange = totalExpenseSpent - lastMonthTotalSpent;
            decimal? monthOverMonthChangePercent = lastMonthTotalSpent > 0
                ? (monthOverMonthChange / lastMonthTotalSpent) * 100m
                : null;

            // Recent Transactions
            var RecentTransactions = _unitOfWork.Transaction
                .GetRange(t => t.OwnerId == CurrentUserId, "Category")
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToList();

            return Json(new
            {
                totalIncome = totalIncome.ToString("C0"),
                totalExpense = totalExpense.ToString("C0"),
                balance = balance.ToString("C0"),
                budgetSummary = new
                {
                    totalBudget = totalBudget.ToString("C0"),
                    totalBudgetSpent = totalBudgetSpent.ToString("C0"),
                    totalExpenseSpent = totalExpenseSpent.ToString("C0"),
                    totalBudgetRemaining = totalBudgetRemaining.ToString("C0"),
                    totalBudgetRemainingPercent = totalBudgetRemainingPercent,
                    lastMonthSpent = lastMonthTotalSpent.ToString("C0"),
                    thisMonthExpenseAmount = totalExpenseSpent,
                    lastMonthExpenseAmount = lastMonthTotalSpent,
                    monthOverMonthChange = monthOverMonthChange.ToString("C0"),
                    monthOverMonthChangeAmount = monthOverMonthChange,
                    monthOverMonthChangePercent = monthOverMonthChangePercent,
                    warningCount = budgetData.Count(x => x.OverBudget)
                },
                budgetData = budgetData,
                chartData = expenseChartData,
                splineData = SplineChartData,
                recentTransactionData = RecentTransactions
            });
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }

    public class SplineChartData
    {
        public string day;
        public decimal income;
        public decimal expense;
    }

    public class DashboardBudgetData
    {
        public string CategoryTitle { get; set; } = string.Empty;
        public decimal Limit { get; set; }
        public decimal Spent { get; set; }
        public decimal LastMonthSpent { get; set; }
        public decimal Remaining { get; set; }
        public bool HasLimit { get; set; }
        public bool OverBudget { get; set; }
        public bool NearLimit { get; set; }
        public decimal ProgressPercent { get; set; }
        public string LimitText { get; set; } = string.Empty;
        public string SpentText { get; set; } = string.Empty;
        public string LastMonthSpentText { get; set; } = string.Empty;
        public string RemainingText { get; set; } = string.Empty;
    }
}