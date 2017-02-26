using Peril.Api.Repository;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peril.Api.Tests.Repository
{
    internal class DummyBatchOperationHandle : IBatchOperationHandle
    {
        public delegate void QueuedOperation();

        public DummyBatchOperationHandle()
        {
            QueuedOperations = new List<QueuedOperation>();
        }

        public int RemainingCapacity
        {
            get
            {
                return 100 - QueuedOperations.Count;
            }
        }

        public void Dispose()
        {
            CommitBatch();
        }

        public Task CommitBatch()
        {
            foreach (QueuedOperation operation in QueuedOperations)
            {
                operation();
            }
            QueuedOperations.Clear();

            return Task.FromResult(0);
        }

        internal List<QueuedOperation> QueuedOperations { get; private set; }
    }
}
