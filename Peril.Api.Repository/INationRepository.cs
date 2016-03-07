using System;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public interface INationRepository
    {
        Task<INationData> GetNation(String userId);
    }
}
