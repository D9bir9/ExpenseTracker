using ExpenseTracker.Data.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using ExpenseTracker.Models;


namespace ExpenseTracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public DashboardController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        
        [HttpGet]
        public IActionResult GetDashboardData(string dateRange = "7days")
        {
            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;
            int rangeEnd = 7;

            switch (dateRange?.ToLower())
            {
                case "14days":
                    StartDate = DateTime.Today.AddDays(-13);
                    rangeEnd = 14;
                    break;
                case "30days":
                    StartDate = DateTime.Today.AddDays(-29);
                    rangeEnd = 30;
                    break;
                case "thismonth":
                    StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    rangeEnd = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
                    break;
            }

            List<Transaction> selectedTransactions = _unitOfWork.Transaction
                .GetRange(y => y.Date >= StartDate && y.Date <= EndDate, "Category")
                .ToList();


            int totalIncome = selectedTransactions.Where(t => t.Category.Type == "Income").Sum(t => t.Amount);
            int totalExpense = selectedTransactions.Where(t => t.Category.Type == "Expense").Sum(t => t.Amount);
            int balance = totalIncome - totalExpense;

            // doughnut chart
            var expenseChartData = selectedTransactions
                .Where(t => t.Category.Type == "Expense")
                .GroupBy(t => t.Category.CategoryId)
                .Select(k => new {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("c0")
                })
                .OrderBy(t=> t.amount)
                .ToList();

            // spline chart Income vs Expense
            // Income
            List<SplineChartData> IncomeSummary = selectedTransactions
                .Where(t => t.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount),
                }).ToList();

            // Expense
            List<SplineChartData> ExpenseSummary = selectedTransactions
                .Where(t => t.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(l => l.Amount),
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
                select(new
                {
                    day = day,
                    income = income == null? 0: income.income,
                    expense = expense == null? 0: expense.expense
                });

            // Recent Transactions
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
        public int income;
        public int expense;
    }
}