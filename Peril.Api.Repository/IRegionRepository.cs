using System;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public interface IRegionRepository
    {
        Task<IRegionData> GetRegion(Guid regionId);
    }
}
