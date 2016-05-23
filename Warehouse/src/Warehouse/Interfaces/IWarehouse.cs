using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace Warehouse.Interfaces
{
    public interface IWarehouse<T>
    {
        void Initialize(string dbName, string collection);
        Task<T> Get(string Id);
        Task<List<T>> GetAll(Expression<Func<T, bool>> predicate);
        void Store(T obj, string Id);
        void Delete(string Id);
        void Update(T obj, string Id);
    }
}
