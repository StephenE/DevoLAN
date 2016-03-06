using Peril.Core;
using System;

namespace Peril.Api.Models
{
    public class Session : ISession
    {
        public Guid GameId { get; set; }
        public Guid PhaseId { get; set; }
        public SessionPhase PhaseType { get; set; }
    }
}