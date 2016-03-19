using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peril.Api.Repository;
using Peril.Api.Tests.Controllers;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peril.Api.Tests.Repository
{
    public class DummyWorldRepository : IWorldRepository
    {
        public DummyWorldRepository()
        {
            BorderClashes = new Dictionary<Guid, DummyCombat>();
            MassInvasions = new Dictionary<Guid, DummyCombat>();
            Invasions = new Dictionary<Guid, DummyCombat>();
            SpoilsOfWar = new Dictionary<Guid, DummyCombat>();
        }

        public Task<IEnumerable<ICombat>> GetCombat(Guid sessionId)
        {
            List<ICombat> combat = BorderClashes.Select(combatPair => combatPair.Value as ICombat).ToList();
            combat.AddRange(MassInvasions.Select(combatPair => combatPair.Value));
            combat.AddRange(Invasions.Select(combatPair => combatPair.Value));
            combat.AddRange(SpoilsOfWar.Select(combatPair => combatPair.Value));
            return Task.FromResult<IEnumerable<ICombat>>(combat);
        }

        public Dictionary<Guid, DummyCombat> BorderClashes { get; private set; }
        public Dictionary<Guid, DummyCombat> MassInvasions { get; private set; }
        public Dictionary<Guid, DummyCombat> Invasions { get; private set; }
        public Dictionary<Guid, DummyCombat> SpoilsOfWar { get; private set; }
    }

    static class ControllerMockWorldRepositoryExtensions
    {
        static public ControllerMockSetupContext SetupBorderClash(this ControllerMockSetupContext setupContext, Guid attackingRegion, UInt32 attackingTroops, Guid secondAttackingRegion, UInt32 secondAttackingTroops)
        {
            Guid combatId = Guid.NewGuid();
            DummyCombat combat = new DummyCombat(combatId, CombatType.BorderClash);
            combat.SetupAddArmy(attackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[attackingRegion].OwnerId, CombatArmyMode.Attacking, attackingTroops);
            combat.SetupAddArmy(secondAttackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[secondAttackingRegion].OwnerId, CombatArmyMode.Attacking, secondAttackingTroops);
            setupContext.ControllerMock.WorldRepository.BorderClashes[combatId] = combat;
            return setupContext;
        }

        static public ControllerMockSetupContext SetupMassInvasion(this ControllerMockSetupContext setupContext, Guid targetRegion, UInt32 defendingTroops, Guid attackingRegion, UInt32 attackingTroops, Guid secondAttackingRegion, UInt32 secondAttackingTroops)
        {
            Guid combatId = Guid.NewGuid();
            DummyCombat combat = new DummyCombat(combatId, CombatType.MassInvasion);
            combat.SetupAddArmy(attackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[attackingRegion].OwnerId, CombatArmyMode.Attacking, attackingTroops);
            combat.SetupAddArmy(secondAttackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[secondAttackingRegion].OwnerId, CombatArmyMode.Attacking, secondAttackingTroops);
            combat.SetupAddArmy(targetRegion, setupContext.ControllerMock.RegionRepository.RegionData[targetRegion].OwnerId, CombatArmyMode.Defending, defendingTroops);
            setupContext.ControllerMock.WorldRepository.MassInvasions[combatId] = combat;
            return setupContext;
        }

        static public ControllerMockSetupContext SetupInvasion(this ControllerMockSetupContext setupContext, Guid attackingRegion, UInt32 attackingTroops, Guid defendingRegion, UInt32 defendingTroops)
        {
            Guid combatId = Guid.NewGuid();
            DummyCombat combat = new DummyCombat(combatId, CombatType.Invasion);
            combat.SetupAddArmy(attackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[attackingRegion].OwnerId, CombatArmyMode.Attacking, attackingTroops);
            combat.SetupAddArmy(defendingRegion, setupContext.ControllerMock.RegionRepository.RegionData[defendingRegion].OwnerId, CombatArmyMode.Defending, defendingTroops);
            setupContext.ControllerMock.WorldRepository.Invasions[combatId] = combat;
            return setupContext;
        }

        static public ControllerMockSetupContext SetupSpoilsOfWar(this ControllerMockSetupContext setupContext, Guid targetRegion, Guid attackingRegion, UInt32 attackingTroops, Guid secondAttackingRegion, UInt32 secondAttackingTroops)
        {
            Guid combatId = Guid.NewGuid();
            DummyCombat combat = new DummyCombat(combatId, CombatType.SpoilsOfWar);
            combat.SetupAddArmy(attackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[attackingRegion].OwnerId, CombatArmyMode.Attacking, attackingTroops);
            combat.SetupAddArmy(secondAttackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[secondAttackingRegion].OwnerId, CombatArmyMode.Attacking, secondAttackingTroops);
            combat.SetupAddArmy(targetRegion, setupContext.ControllerMock.RegionRepository.RegionData[targetRegion].OwnerId, CombatArmyMode.Defending, 0);
            setupContext.ControllerMock.WorldRepository.SpoilsOfWar[combatId] = combat;
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
