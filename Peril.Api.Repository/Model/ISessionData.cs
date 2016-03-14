using Peril.Core;
using System;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Model
{
    public interface ISessionData : ISession
    {
        String OwnerId { get; }

        String CurrentEtag { get; }
    }
}
