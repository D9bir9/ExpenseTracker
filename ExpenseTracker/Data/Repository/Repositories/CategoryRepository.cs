using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpenseTracker.Data.Repository.IRepository;
using ExpenseTracker.Models;

namespace ExpenseTracker.Data.Repository.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext db) : base(db)
        {
        }
        public void Update(Category category)
        {
            var categoryFromDb = GetById(c => c.CategoryId.Equals(category.CategoryId));
            if (categoryFromDb != null)
            {
                categoryFromDb.Title = category.Title;
                categoryFromDb.Icon = category.Icon;
                categoryFromDb.Type = category.Type;
            }
        }
    }
}