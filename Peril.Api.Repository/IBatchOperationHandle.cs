using System;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public interface IBatchOperationHandle : IDisposable
    {
        Int32 RemainingCapacity { get; }

        Task CommitBatch();

        Task Abort();
    }
}
