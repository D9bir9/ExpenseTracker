using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpenseTracker.Data.Repository.IRepository;

namespace ExpenseTracker.Data.Repository.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        public ICategoryRepository Category { get ; private set ; }
        public ITransactionRepository Transaction { get; private set; }
        private ApplicationDbContext _db;

        public UnitOfWork(ApplicationDbContext db)
        {
            Category = new CategoryRepository(db);
            Transaction = new TransactionRepository(db);
            _db = db;
        }

        public void Save()
        {
            _db.SaveChanges();
        }
    }
}