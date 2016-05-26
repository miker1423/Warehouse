using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace Warehouse.Interfaces
{
    public interface IWarehouse<T>
    {

        /// <summary>
        /// Initializes the database and collection, it they dont exists it will be created
        /// </summary>
        /// <param name="dbName">Database name to connect to</param>
        /// <param name="collection">Collection to connect to</param>
        void Initialize(string dbName, string collection);
        /// <summary>
        /// Retrive just one JSON document
        /// </summary>
        /// <param name="Id">Id of the object looking for</param>
        /// <returns>A single object</returns>
        Task<T> Get(string Id);
        /// <summary>
        /// Retrieves all documents within a collection
        /// </summary>
        /// <returns>List of objects</returns>
        List<T> GetAll();
        /// <summary>
        /// Retrive all the JSON documents
        /// </summary>
        /// <param name="predicate">Query predicate</param>
        /// <returns>List of objects defined when instanciated the class</returns>
        Task<List<T>> GetAll(Expression<Func<T, bool>> predicate);
        /// <summary>
        /// Creates a JSON document in DB
        /// </summary>
        /// <param name="obj">Object to be stored</param>
        /// <param name="Id">Id of the object</param>
        Task Store(T obj, string Id);
        /// <summary>
        /// Deletes a JSON document
        /// </summary>
        /// <param name="Id">Id of the odcument to be deleted</param>
        Task Delete(string Id);
        /// <summary>
        /// This method updates a JSON document
        /// </summary>
        /// <param name="obj">new JSON to be stored</param>
        /// <param name="Id">Id of the object to update</param>
        Task Update(T obj, string Id);
        /// <summary>
        /// Deletes all documents within a collection
        /// </summary>
        /// <returns></returns>
        Task CleanCollection();
    }
}
