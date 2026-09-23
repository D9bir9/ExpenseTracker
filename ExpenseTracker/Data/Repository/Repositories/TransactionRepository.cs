using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpenseTracker.Models;
using ExpenseTracker.Data.Repository.IRepository;

namespace ExpenseTracker.Data.Repository.Repositories
{
    public class TransactionRepository : Repository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(ApplicationDbContext db) : base(db)
        {
        }

        public void Update(Transaction transaction)
        {
            var transactionFromDb = GetById(t => t.TransactionId.Equals(transaction.TransactionId), "Category");
            if (transactionFromDb != null)
            {
                transactionFromDb.CategoryId = transaction.CategoryId;
                transactionFromDb.Amount = transaction.Amount;
                transactionFromDb.Note = transaction.Note;
                transactionFromDb.Date = transaction.Date;
            }
        }
    }
}