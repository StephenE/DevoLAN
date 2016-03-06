using System;

namespace Peril.Api.Tests.Repository
{
    class DummyPlayer
    {
        public DummyPlayer(String userId)
        {
            UserId = userId;
            CompletedPhase = Guid.Empty;
        }

        public String UserId { get; set; }

        public Guid CompletedPhase { get; set; }
    }
}
