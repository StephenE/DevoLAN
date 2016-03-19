using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peril.Api.Repository;
using Peril.Api.Tests.Controllers;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peril.Api.Tests.Repository
{
    public class DummyWorldRepository : IWorldRepository
    {
        public DummyWorldRepository()
        {
            BorderClashes = new Dictionary<Guid, ICombat>();
            MassInvasions = new Dictionary<Guid, ICombat>();
            Invasions = new Dictionary<Guid, ICombat>();
            SpoilsOfWar = new Dictionary<Guid, ICombat>();
        }

        public IEnumerable<ICombat> GetCombat(Guid sessionId)
        {
            throw new NotImplementedException();
        }

        public Dictionary<Guid, ICombat> BorderClashes { get; private set; }
        public Dictionary<Guid, ICombat> MassInvasions { get; private set; }
        public Dictionary<Guid, ICombat> Invasions { get; private set; }
        public Dictionary<Guid, ICombat> SpoilsOfWar { get; private set; }
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

        static public ICombat GetInvasion(this ControllerMock controller, Guid defendingRegion)
        {
            var query = from combatEntry in controller.WorldRepository.Invasions
                        let combat = combatEntry.Value
                        let defendingArmy = combat.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).FirstOrDefault()
                        where defendingArmy.OriginRegionId == defendingRegion
                        select combat;

            return query.FirstOrDefault();
        }
    }
    
    static class AssertCombat
    {
        static public void IsAttacking(Guid regionId, UInt32 numberOfTroops, String ownerId, ICombat combat)
        {
            var atatckingArmy = combat.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Attacking && army.OriginRegionId == regionId);

            Assert.AreEqual(1, atatckingArmy.Count(), "Could not find attacking army");
            Assert.AreEqual(numberOfTroops, atatckingArmy.First().NumberOfTroops);
            Assert.AreEqual(ownerId, atatckingArmy.First().OwnerUserId);
        }

        static public void IsDefending(Guid regionId, UInt32 numberOfTroops, String ownerId, ICombat combat)
        {
            var defendingArmy = combat.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).FirstOrDefault();

            Assert.IsNotNull(defendingArmy, "No defending army in combat");
            Assert.AreEqual(numberOfTroops, defendingArmy.NumberOfTroops);
            Assert.AreEqual(regionId, defendingArmy.OriginRegionId);
            Assert.AreEqual(ownerId, defendingArmy.OwnerUserId);
        }
    }
}
