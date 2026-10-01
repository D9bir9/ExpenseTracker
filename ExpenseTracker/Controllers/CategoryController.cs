using ExpenseTracker.Data.Repository.IRepository;
using ExpenseTracker.Models;
using ExpenseTracker.Services;
using Microsoft.AspNetCore.Mvc;


namespace ExpenseTracker.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrencyConversionService _currencyService;

        public CategoryController(IUnitOfWork unitOfWork, ICurrencyConversionService currencyService)
        {
            _unitOfWork = unitOfWork;
            _currencyService = currencyService;
        }

        public async Task<IActionResult> Index()
        {
            var categories = _unitOfWork.Category.GetAll("").ToList();
            var targetCurrency = _currencyService.GetCurrencyCode(
                System.Globalization.CultureInfo.CurrentCulture.Name);
            await Task.WhenAll(categories.Select(async category =>
            {
                var displayLimit = await _currencyService.ConvertAsync(
                    category.MonthlyBudgetLimit, "USD", targetCurrency);
                category.MonthlyBudgetLimitFormatted = displayLimit.ToString("C2");
            }));
            return View(categories);
        }

        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            if (id == 0)
            {
                return View(new Category());
            }
            Category? categoryFromDb = _unitOfWork.Category.GetById(c => c.CategoryId.Equals(id),"");
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            var targetCurrency = _currencyService.GetCurrencyCode(
                System.Globalization.CultureInfo.CurrentCulture.Name);
            categoryFromDb.MonthlyBudgetLimit = await _currencyService.ConvertAsync(
                categoryFromDb.MonthlyBudgetLimit, "USD", targetCurrency);
            return View(categoryFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(Category obj)
        {
            obj.Title = obj.Title?.Trim();
            obj.Icon = obj.Icon?.Trim();

            if (string.IsNullOrWhiteSpace(obj.Title))
            {
                ModelState.AddModelError(nameof(Category.Title), "Please enter a category name.");
            }

            if (obj.MonthlyBudgetLimit < 0)
            {
                ModelState.AddModelError(nameof(Category.MonthlyBudgetLimit), "Monthly budget cannot be negative.");
            }

            if (ModelState.IsValid)
            {
                var targetCurrency = _currencyService.GetCurrencyCode(
                    System.Globalization.CultureInfo.CurrentCulture.Name);
                obj.MonthlyBudgetLimit = await _currencyService.ConvertAsync(
                    obj.MonthlyBudgetLimit, targetCurrency, "USD");

                if(obj.CategoryId == 0)
                {
                    _unitOfWork.Category.Create(obj);
                }
                else
                {
                    _unitOfWork.Category.Update(obj);
                }
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            return View(obj);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            Category? obj = _unitOfWork.Category.GetById(u => u.CategoryId.Equals(id), "");
            if(obj == null)
            {
                return NotFound();
            }
            _unitOfWork.Category.Delete(obj);
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}