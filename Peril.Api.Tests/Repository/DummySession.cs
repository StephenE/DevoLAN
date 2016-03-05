using Peril.Core;
using System;
using System.Collections.Generic;

namespace Peril.Api.Tests.Repository
{
    class DummySession : ISession
    {
        public DummySession()
        {
            Players = new List<DummyPlayer>();
        }

        public Guid GameId { get; set; }

        public List<DummyPlayer> Players { get;set; }
    }
}
