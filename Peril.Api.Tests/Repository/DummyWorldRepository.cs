using Peril.Api.Repository;
using Peril.Api.Tests.Controllers;
using Peril.Core;
using System;
using System.Collections.Generic;

namespace Peril.Api.Tests.Repository
{
    public class DummyWorldRepository : IWorldRepository
    {
        public DummyWorldRepository()
        {
            BorderClashes = new List<ICombat>();
            MassInvasions = new List<ICombat>();
            Invasions = new List<ICombat>();
            SpoilsOfWar = new List<ICombat>();
        }

        public IEnumerable<ICombat> GetCombat(Guid sessionId)
        {
            throw new NotImplementedException();
        }

        public List<ICombat> BorderClashes { get; private set; }
        public List<ICombat> MassInvasions { get; private set; }
        public List<ICombat> Invasions { get; private set; }
        public List<ICombat> SpoilsOfWar { get; private set; }
    }

    static class ControllerMockWorldRepositoryExtensions
    {
        static public ControllerMockSetupContext SetupBorderClash(this ControllerMockSetupContext setupContext, Guid attackingRegion, UInt32 attackingTroops, Guid secondAttackingRegion, UInt32 secondAttackingTroops)
        {
            return setupContext;
        }

        static public ControllerMockSetupContext SetupMassInvasion(this ControllerMockSetupContext setupContext, Guid targetRegion, UInt32 defendingTroops, Guid attackingRegion, UInt32 attackingTroops, Guid secondAttackingRegion, UInt32 secondAttackingTroops)
        {
            return setupContext;
        }

        static public ControllerMockSetupContext SetupInvasion(this ControllerMockSetupContext setupContext, Guid attackingRegion, UInt32 attackingTroops, Guid defendingRegion, UInt32 defendingTroops)
        {
            return setupContext;
        }

        static public ControllerMockSetupContext SetupSpoilsOfWar(this ControllerMockSetupContext setupContext, Guid targetRegion, Guid attackingRegion, UInt32 attackingTroops, Guid secondAttackingRegion, UInt32 secondAttackingTroops)
        {
            return setupContext;
        }
    }
}
