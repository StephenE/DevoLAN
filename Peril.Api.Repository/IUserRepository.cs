using System.Data.Entity;

namespace Peril.Api.Repository
{
    public interface IUserRepository
    {
        IDbSet<ApplicationUser> Users { get; }
    }
}
