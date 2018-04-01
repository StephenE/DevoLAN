using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure
{
    public class BatchOperationHandle : IBatchOperationHandle
    {
        public BatchOperationHandle(CloudTable table)
        {
            TargetTable = table;
            PrerequisiteOperation = new List<Task>();
            StartNewBatch();
        }

        public void Dispose()
        {
            var task = CommitBatch();
            task.Wait();
        }

        public Int32 RemainingCapacity
        {
            get
            {
                return 100 - (BatchOperation.Count + ReservedBatchCapacity);
            }
        }

        private void StartNewBatch()
        {
            BatchOperation = new TableBatchOperation();
            PrerequisiteOperation.Clear();
            ReservedBatchCapacity = 0;
        }

        internal void AddPrerequisite(Task prerequisiteOperation, Int32 reservedBatchCapacity)
        {
            ReservedBatchCapacity += reservedBatchCapacity;
            PrerequisiteOperation.Add(prerequisiteOperation);
            prerequisiteOperation.ContinueWith(task => ReservedBatchCapacity -= reservedBatchCapacity);
        }

        public async Task CommitBatch()
        {
            await Task.WhenAll(PrerequisiteOperation);

            if (BatchOperation.Count > 0)
            {
                try
                {
                    await TargetTable.ExecuteBatchAsync(BatchOperation);
                }
                catch (StorageException exception)
                {
                    if (exception.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
                    {
                        throw new ConcurrencyException();
                    }
                    else
                    {
                        throw exception;
                    }
                }
            }

            StartNewBatch();
        }

        public async Task Abort()
        {
            await Task.WhenAll(PrerequisiteOperation);

            StartNewBatch();
        }

        private CloudTable TargetTable { get; set; }
        private List<Task> PrerequisiteOperation { get; set; }
        private Int32 ReservedBatchCapacity { get; set; }
        public TableBatchOperation BatchOperation { get; private set; }
    }
}
