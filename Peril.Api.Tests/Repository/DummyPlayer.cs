using Peril.Core;
using System;

namespace Peril.Api.Tests.Repository
{
    class DummyPlayer : IPlayer
    {
        public DummyPlayer(String userId)
        {
            UserId = userId;
            CompletedPhase = Guid.Empty;
        }

        public String UserId { get; set; }

        public String Name
        {
            get { throw new NotImplementedException("Get the Name from the UserRepository"); }
        }

        public Guid CompletedPhase { get; set; }

        public PlayerColour Colour { get; set; }
    }
}
