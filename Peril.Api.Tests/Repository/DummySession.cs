﻿using Peril.Core;
using System;
using System.Collections.Generic;

namespace Peril.Api.Tests.Repository
{
    class DummySession : ISession
    {
        public DummySession()
        {
            Players = new List<DummyPlayer>();
            PhaseId = Guid.Empty;
            PhaseType = SessionPhase.NotStarted;
        }

        public Guid GameId { get; set; }

        public List<DummyPlayer> Players { get;set; }

        public Guid PhaseId { get; set; }

        public SessionPhase PhaseType { get; set; }

        #region - Test Setup Helpers -
        internal DummySession SetupSessionPhase(SessionPhase round)
        {
            PhaseType = round;
            PhaseId = Guid.NewGuid();
            return this;
        }

        internal DummySession SetupAddPlayer(String userId)
        {
            Players.Add(new DummyPlayer(userId));
            return this;
        }
        #endregion
    }
}
