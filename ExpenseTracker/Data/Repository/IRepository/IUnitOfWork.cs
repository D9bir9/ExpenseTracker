using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpenseTracker.Data.Repository.IRepository
{
    public interface IUnitOfWork
    {
        public ICategoryRepository Category { get;}
        public ITransactionRepository Transaction { get; }

        void Save();
    }
}