using ExpenseTracker.Models;
using ExpenseTracker.Data.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ExpenseTracker.Controllers
{
    [Authorize]
    public class TransactionController : UserScopedController
    {
         private readonly IUnitOfWork _unitOfWork;

        public TransactionController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index(int? categoryId = null, string sortBy = "date-desc")
        {
            var transactions = _unitOfWork.Transaction.GetRange(t => t.OwnerId == CurrentUserId, "Category")
                .AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                transactions = transactions.Where(t => t.CategoryId == categoryId.Value);
            }

            transactions = sortBy switch
            {
                "amount-desc" => transactions.OrderByDescending(t => t.Amount),
                "amount-asc" => transactions.OrderBy(t => t.Amount),
                "date-asc" => transactions.OrderBy(t => t.Date),
                _ => transactions.OrderByDescending(t => t.Date),
            };

            ViewBag.SelectedCategoryId = categoryId ?? 0;
            ViewBag.SelectedSort = sortBy;
            ViewBag.Categories = _unitOfWork.Category.GetRange(c => c.OwnerId == CurrentUserId).ToList();

            return View(transactions.ToList());
        }


        public IActionResult AddOrEdit(int id = 0)
        {
            populateCategories();
            if (id == 0)
            {
                return View(new Transaction());
            }
            Transaction? transactionFromDb = _unitOfWork.Transaction.GetById(
                t => t.TransactionId == id && t.OwnerId == CurrentUserId, "Category");
            if (transactionFromDb == null)
            {
                return NotFound();
            }
            return View(transactionFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddOrEdit(Transaction obj)
        {
            Transaction? existing = null;
            if (obj.TransactionId != 0)
            {
                existing = _unitOfWork.Transaction.GetById(
                    t => t.TransactionId == obj.TransactionId && t.OwnerId == CurrentUserId, "");
                if (existing == null)
                    return NotFound();
            }

            obj.Note = obj.Note?.Trim();

            if (string.IsNullOrWhiteSpace(obj.Note))
            {
                obj.Note = null;
            }

            if (ModelState.IsValid)
            {
                var category = _unitOfWork.Category.GetById(
                    c => c.CategoryId == obj.CategoryId && c.OwnerId == CurrentUserId, "");
                if (category == null)
                {
                    ModelState.AddModelError(nameof(Transaction.CategoryId), "Select one of your categories.");
                }
                else
                {
                    if (existing == null)
                    {
                        obj.OwnerId = CurrentUserId;
                        obj.CultureCode = System.Globalization.CultureInfo.CurrentCulture.Name;
                        _unitOfWork.Transaction.Create(obj);
                    }
                    else
                    {
                        existing.CategoryId = obj.CategoryId;
                        existing.Amount = obj.Amount;
                        existing.Date = obj.Date;
                        existing.Note = obj.Note;
                        _unitOfWork.Transaction.Update(existing);
                    }

                    _unitOfWork.Save();
                    return RedirectToAction("Index");
                }
            }
            populateCategories();
            return View(obj);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            Transaction? obj = _unitOfWork.Transaction.GetById(
                t => t.TransactionId == id && t.OwnerId == CurrentUserId, "Category");
            if(obj == null)
            {
                return NotFound();
            }
            _unitOfWork.Transaction.Delete(obj);
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

        [NonAction]
        public void populateCategories()
        {
            var categoryCollections = _unitOfWork.Category.GetRange(c => c.OwnerId == CurrentUserId).ToList();
            var Default = new Category(){CategoryId = 0, Title = "Select Category"};
            categoryCollections.Insert(0, Default);
            ViewBag.Categories = categoryCollections;
        }
    }
}