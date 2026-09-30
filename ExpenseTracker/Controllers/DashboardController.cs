using ExpenseTracker.Data.Repository.IRepository;
using ExpenseTracker.Services;
using Microsoft.AspNetCore.Mvc;
using ExpenseTracker.Models;

namespace ExpenseTracker.Controllers
{
    public class DashboardController : Controller
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
                .GetRange(y => y.Date >= StartDate && y.Date <= EndDate, "Category")
                .ToList();

            // Convert every transaction's amount into targetCurrency BEFORE any
            // aggregation happens. Each transaction's original currency comes from
            // the CultureCode that was stamped on it when it was created (see
            // Transaction model + Create action changes noted separately).
            var convertedAmounts = new Dictionary<int, decimal>();
            foreach (var t in selectedTransactions)
            {
                string sourceCurrency = _currencyService.GetCurrencyCode(t.CultureCode ?? "en-US");
                convertedAmounts[t.TransactionId] =
                    await _currencyService.ConvertAsync(t.Amount, sourceCurrency, targetCurrency);
            }

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

            // Recent Transactions
            // Note: these are shown in their ORIGINAL currency/amount, not converted.
            // If you want recent-transaction amounts converted too, apply the same
            // per-transaction conversion used above before returning them.
            var RecentTransactions = _unitOfWork.Transaction.GetAll("Category")
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToList();

            return Json(new
            {
                totalIncome = totalIncome.ToString("C0"),
                totalExpense = totalExpense.ToString("C0"),
                balance = balance.ToString("C0"),
                chartData = expenseChartData,
                splineData = SplineChartData,
                recentTransactionData = RecentTransactions
            });
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
}