﻿using System;
using System.Threading.Tasks;
using KnightBus.Core.Sagas;
using KnightBus.Core.Sagas.Exceptions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace KnightBus.Azure.Storage.Sagas
{
    public class StorageTableSagaStore : ISagaStore
    {
        private readonly CloudTable _table;
        private const string TableName = "sagas";

        public StorageTableSagaStore(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference(TableName);
        }

        public async Task<T> GetSaga<T>(string partitionKey, string id)
        {
            var operation = TableOperation.Retrieve<SagaTableData>(partitionKey, id);
            var result = await _table.ExecuteAsync(operation).ConfigureAwait(false);
            if(result.HttpStatusCode == 404) throw new SagaNotFoundException();

            return JsonConvert.DeserializeObject<T>(((SagaTableData)result.Result).Json);
        }

        public async Task<T> Create<T>(string partitionKey, string id, T sagaData)
        {
            var operation = TableOperation.Insert(new SagaTableData
            {
                PartitionKey = partitionKey,
                RowKey = id,
                Json = JsonConvert.SerializeObject(sagaData)
            });

            var result = await OptimisticExecuteAsync(() => _table.ExecuteAsync(operation)).ConfigureAwait(false);
            if(result.HttpStatusCode == 409) throw new SagaAlreadyStartedException();
            if(result.HttpStatusCode < 200 || result.HttpStatusCode > 299) throw new SagaStorageFailedException();
            
            return JsonConvert.DeserializeObject<T>(((SagaTableData)result.Result).Json);
        }

        public async Task Update<T>(string partitionKey, string id, T sagaData)
        {
            var operation = TableOperation.Replace(new SagaTableData
            {
                PartitionKey = partitionKey,
                RowKey = id,
                Json = JsonConvert.SerializeObject(sagaData)
            });

            var result = await _table.ExecuteAsync(operation).ConfigureAwait(false);
            if (result.HttpStatusCode == 404) throw new SagaNotFoundException();
            if (result.HttpStatusCode < 200 || result.HttpStatusCode > 299) throw new SagaStorageFailedException();
        }

        public async Task Complete(string partitionKey, string id)
        {
            var operation = TableOperation.Delete(new SagaTableData {PartitionKey = partitionKey, RowKey = id});
            var result = await _table.ExecuteAsync(operation);
            if (result.HttpStatusCode < 200 || result.HttpStatusCode > 299) throw new SagaStorageFailedException();
        }

        private async Task<TableResult> OptimisticExecuteAsync(Func<Task<TableResult>> func)
        {
            var result = await func.Invoke().ConfigureAwait(false);
            if (result.HttpStatusCode != 404) return result;

            await _table.CreateIfNotExistsAsync().ConfigureAwait(false);
            return await func.Invoke().ConfigureAwait(false);
        }
    }
}