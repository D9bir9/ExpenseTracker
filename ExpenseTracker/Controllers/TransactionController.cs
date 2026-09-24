using ExpenseTracker.Models;
using ExpenseTracker.Data.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers
{
    public class TransactionController : Controller
    {
         private readonly IUnitOfWork _unitOfWork;

        public TransactionController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Transaction> transactions = _unitOfWork.Transaction.GetAll("Category");
            return View(transactions);
        }


        public IActionResult AddOrEdit(int id = 0)
        {
            populateCategories();
            if (id == 0)
            {
                return View(new Transaction());
            }
            Transaction? transactionFromDb = _unitOfWork.Transaction.GetById(c => c.TransactionId.Equals(id),"Category");
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
            if (ModelState.IsValid)
            {
                if(obj.TransactionId == 0)
                {
                   
                    obj.CultureCode = System.Globalization.CultureInfo.CurrentCulture.Name;
                    _unitOfWork.Transaction.Create(obj);
                }
                else
                {
                    // preserve the original CultureCode on edit — reload it from the DB
                    // rather than trusting the hidden form field, since a user could tamper with it
                    var existing = _unitOfWork.Transaction.GetById(t => t.TransactionId == obj.TransactionId, "");
                    obj.CultureCode = existing?.CultureCode;
                    _unitOfWork.Transaction.Update(obj);
                }
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            populateCategories();
            return View(obj);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            Transaction? obj = _unitOfWork.Transaction.GetById(u => u.TransactionId.Equals(id), "Category");
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
            var categoryCollections = _unitOfWork.Category.GetAll("").ToList();
            var Default = new Category(){CategoryId = 0, Title = "Select Category"};
            categoryCollections.Insert(0, Default);
            ViewBag.Categories = categoryCollections;
        }
    }
}