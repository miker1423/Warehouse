using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;

using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

using Warehouse.Interfaces;

namespace Warehouse.Implementation
{
    public class Warehouse<T> : IWarehouse<T>
    {
        #region Variables
        private string _dbName;
        private string _collection;
        private DocumentClient _client;
        private string _partitionKey;
        #endregion

        #region CTOR
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Connection string of the Azure Document DB</param>
        /// <param name="primaryKey">Primary key of Azure Document DB</param>
        public Warehouse(string connectionString, string primaryKey)
            : this(new DocumentClient(new Uri(connectionString), primaryKey))
        {
        }

        internal Warehouse(DocumentClient client)
        {
            _client = client;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the database and collection, it they dont exists it will be created
        /// </summary>
        /// <param name="dbName">Database name to connect to</param>
        /// <param name="collection">Collection to connect to</param>
        public async Task Initialize(string dbName, string collection)
        {
            _dbName = dbName;
            _collection = collection;

            await CreateDBIfNotExists();
            await CreateDocumentCollectionIfNotExists();
        }

        public async Task Initialize(string dbName, string collection, string partitionKey)
        {
            _dbName = dbName;
            _collection = collection;
            _partitionKey = partitionKey;

            await CreateDBIfNotExists();
            await CreateDocumentCollectionWithPartitionKeyIfNotExists();
        }

        /// <summary>
        /// Deletes a JSON document
        /// </summary>
        /// <param name="Id">Id of the odcument to be deleted</param>
        public async Task Delete(string Id)
        {
            try
            {
                await _client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(_dbName, _collection, Id));
            }
            catch (DocumentClientException de)
            {
                throw de;
            }
        }

        /// <summary>
        /// Retrive just one JSON document
        /// </summary>
        /// <param name="Id">Id of the object looking for</param>
        /// <returns>A single object</returns>
        public async Task<T> Get(string Id)
        {
            try
            {
                var document = await  _client.ReadDocumentAsync(UriFactory.CreateDocumentUri(_dbName, _collection, Id));
                return (T)(dynamic)document.Resource;
            }
            catch (DocumentClientException de)
            {
                if(de.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return default(T);
                }
                else
                {
                    throw de;
                }
            }
        }

        /// <summary>
        /// Retrieves all documents within a collection
        /// </summary>
        /// <returns>List of objects</returns>
        public List<T> GetAll()
        {
            try
            {
                var docuements = _client.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(_dbName, _collection));
                return docuements.ToList();
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw de;
                }
            }
        }

        /// <summary>
        /// Retrive all the JSON documents
        /// </summary>
        /// <param name="predicate">a expression that tells how long to run the query</param>
        /// <returns>List of objects defined when instanciated the class</returns>
        public async Task<List<T>> GetAll(Expression<Func<T, bool>> predicate)
        {
            try
            {
                var query = _client.CreateDocumentQuery<T>(
                    UriFactory.CreateDocumentCollectionUri(_dbName, _collection))
                    .Where(predicate)
                    .AsDocumentQuery();

                var results = new List<T>();
                while (query.HasMoreResults)
                {
                    results.AddRange(await query.ExecuteNextAsync<T>());
                }

                return results;
            }
            catch (DocumentClientException de)
            {
                throw de;
            }
        }

        /// <summary>
        /// Creates a JSON document in DB
        /// </summary>
        /// <param name="obj">Object to be stored</param>
        /// <param name="Id">Id of the object</param>
        /// <returns>Id of element created</returns>
        public async Task<string> Store(T obj, string Id)
        {
            try
            {
                var response = await _client.ReadDocumentAsync(UriFactory.CreateDocumentUri(_dbName, _collection, Id));
                return response.Resource.Id;
            }
            catch (DocumentClientException de)
            {
                if(de.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var response = await _client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(_dbName, _collection), obj);
                    return response.Resource.Id;
                }
                else
                {
                    throw de;
                }
            }
        }

        /// <summary>
        /// This method updates a JSON document
        /// </summary>
        /// <param name="obj">new JSON to be stored</param>
        /// <param name="Id">Id of the object to update</param>
        public async Task Update(T obj, string Id)
        {
            try
            {
                await _client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_dbName, _collection, Id), obj);
            }
            catch (DocumentClientException de)
            {
                throw de;
            }
        }

        /// <summary>
        /// Deletes all documents within a collection
        /// </summary>
        /// <returns></returns>
        public async Task CleanCollection()
        {
            await _client.DeleteDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(_dbName, _collection));
        }
        #endregion

        #region Private Methods
        private async Task CreateDBIfNotExists()
        {
            try
            {
                await _client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(_dbName));
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await _client.CreateDatabaseAsync(new Database { Id = _dbName });
                }
                else
                {
                    throw de;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task CreateDocumentCollectionIfNotExists()
        {
            try
            {
                await _client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(_dbName, _collection));
            }
            catch (DocumentClientException de)
            {
                if(de.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var colletionInfo = new DocumentCollection();
                    colletionInfo.Id = _collection;

                    colletionInfo.IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 });
                    
                    await _client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(_dbName),
                        new DocumentCollection { Id = _collection },
                        new RequestOptions { OfferThroughput = 400 });
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task CreateDocumentCollectionWithPartitionKeyIfNotExists()
        {
            try
            {
                await _client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(_dbName, _collection));
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var colletionInfo = new DocumentCollection();
                    colletionInfo.Id = _collection;

                    colletionInfo.IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 });

                    var collection = new DocumentCollection();
                    collection.Id = _collection;
                    collection.PartitionKey.Paths.Add(_partitionKey);

                    await _client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(_dbName),
                        collection,
                        new RequestOptions { OfferThroughput = 400 });
                }
                else
                {
                    throw;
                }
            }

        }
        #endregion
    }
}
