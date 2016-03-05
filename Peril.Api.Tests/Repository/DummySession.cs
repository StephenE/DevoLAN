using Peril.Core;
using System;
using System.Collections.Generic;

namespace Peril.Api.Tests.Repository
{
    class DummySession : ISession
    {
        public DummySession()
        {
            Players = new List<String>();
        }

        public Guid GameId { get; set; }

        public List<String> Players { get;set; }
    }
}
