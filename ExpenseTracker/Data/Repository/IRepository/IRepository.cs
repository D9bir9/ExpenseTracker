using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ExpenseTracker.Data.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> GetAll(string? includeProperties);
        T GetById(Expression<Func<T, bool>> filter, string? includeProperties);
        void Create(T entity);
        void Delete(T entity);
        public IEnumerable<T> GetRange(Expression<Func<T, bool>> filter, string? includeProperties = null);
    }
}