using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpenseTracker.Models;

namespace ExpenseTracker.Data.Repository.IRepository
{
    public interface ITransactionRepository : IRepository<Transaction>
    {
        void Update(Transaction transaction);
    }
}