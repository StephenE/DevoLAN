using System;

namespace Peril.Core
{
    public interface ISession
    {
        Guid GameId { get; }
    }
}
