using ExpenseTracker.Data.Repository.IRepository;
using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc;


namespace ExpenseTracker.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Category> categories = _unitOfWork.Category.GetAll("");
            return View(categories);
        }

        public IActionResult AddOrEdit(int id = 0)
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
            return View(categoryFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddOrEdit(Category obj)
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