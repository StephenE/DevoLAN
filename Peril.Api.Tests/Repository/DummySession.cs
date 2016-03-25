using Peril.Api.Repository.Model;
using Peril.Core;
using System;
using System.Collections.Generic;

namespace Peril.Api.Tests.Repository
{
    class DummySession : ISessionData
    {
        public DummySession()
        {
            Players = new List<DummyNationData>();
            PhaseId = Guid.Empty;
            PhaseType = SessionPhase.NotStarted;
            Round = 1;
            GenerateNewEtag();
        }

        public String OwnerId { get; set; }

        public Guid GameId { get; set; }

        public List<DummyNationData> Players { get;set; }

        public UInt32 Round { get; set; }

        public Guid PhaseId { get; set; }

        public SessionPhase PhaseType { get; set; }

        public String CurrentEtag { get; set; }

        #region - Test Setup Helpers -
        internal DummySession SetupSessionPhase(SessionPhase round)
        {
            PhaseType = round;
            PhaseId = Guid.NewGuid();
            return this;
        }

        internal DummySession SetupAddPlayer(String userId, PlayerColour colour)
        {
            Players.Add(new DummyNationData(userId) { Colour = colour });
            return this;
        }

        internal DummySession GenerateNewEtag()
        {
            CurrentEtag = Guid.NewGuid().ToString();
            return this;
        }
        #endregion
    }
}
