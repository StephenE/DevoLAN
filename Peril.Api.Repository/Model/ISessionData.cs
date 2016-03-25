using Peril.Core;
using System;

namespace Peril.Api.Repository.Model
{
    public interface ISessionData : ISession
    {
        String CurrentEtag { get; }
    }
}
