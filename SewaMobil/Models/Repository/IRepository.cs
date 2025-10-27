using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SewaMobil.Models.Repository
{
    public interface IRepository<T> where T : class
    {
        // Get
        T GetById(int id);
        IEnumerable<T> GetAll();
        IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
        T FirstOrDefault(Expression<Func<T, bool>> predicate);

        // Add
        void Add(T entity);
        void AddRange(IEnumerable<T> entities);

        // Update
        void Update(T entity);

        // Remove
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);

        // Query
        IQueryable<T> Query();
        int Count(Expression<Func<T, bool>> predicate = null);
        bool Any(Expression<Func<T, bool>> predicate);
    }
}
