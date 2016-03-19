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
            NumberGeneratorInjections = new List<int>();
            NumberGenerator = new Random(0);
        }

        public Task<IEnumerable<ICombat>> GetCombat(Guid sessionId)
        {
            List<ICombat> combat = BorderClashes.Select(combatPair => combatPair.Value as ICombat).ToList();
            combat.AddRange(MassInvasions.Select(combatPair => combatPair.Value));
            combat.AddRange(Invasions.Select(combatPair => combatPair.Value));
            combat.AddRange(SpoilsOfWar.Select(combatPair => combatPair.Value));
            return Task.FromResult<IEnumerable<ICombat>>(combat);
        }

        public Task<IEnumerable<ICombat>> GetCombat(Guid sessionId, CombatType combatType)
        {
            switch(combatType)
            {
                case CombatType.BorderClash:
                    return Task.FromResult<IEnumerable<ICombat>>(BorderClashes.Values);
                case CombatType.MassInvasion:
                    return Task.FromResult<IEnumerable<ICombat>>(MassInvasions.Values);
                case CombatType.Invasion:
                    return Task.FromResult<IEnumerable<ICombat>>(Invasions.Values);
                case CombatType.SpoilsOfWar:
                    return Task.FromResult<IEnumerable<ICombat>>(SpoilsOfWar.Values);
            }

            throw new InvalidOperationException();
        }

        public Task AddCombat(Guid sessionId, IEnumerable<Tuple<CombatType, IEnumerable<ICombatArmy>>> armies)
        {
            foreach(var combatData in armies)
            {
                Guid combatId = Guid.NewGuid();
                DummyCombat combat = new DummyCombat(combatId, combatData.Item1);

                foreach(ICombatArmy army in combatData.Item2)
                {
                    combat.SetupAddArmy(army.OriginRegionId, army.OwnerUserId, army.ArmyMode, army.NumberOfTroops);
                }

                switch (combat.ResolutionType)
                {
                    case CombatType.BorderClash:
                        BorderClashes[combatId] = combat;
                        break;
                    case CombatType.MassInvasion:
                        MassInvasions[combatId] = combat;
                        break;
                    case CombatType.Invasion:
                        Invasions[combatId] = combat;
                        break;
                }
            }

            return Task.FromResult(false);
        }

        public Task AddCombatResults(Guid sessionId, IEnumerable<ICombatResult> results)
        {
            throw new NotImplementedException("Not implemented");
        }

        public IEnumerable<Int32> GetRandomNumberGenerator(int minimum, int maximum)
        {
            if (NumberGeneratorInjections.Count > 0)
            {
                while (NumberGeneratorInjections.Count > 0)
                {
                    yield return NumberGeneratorInjections.Last();
                    NumberGeneratorInjections.RemoveAt(NumberGeneratorInjections.Count - 1);
                }
                Assert.Fail("Ran out of pre-generated random numbers");
            }

            yield return NumberGenerator.Next(minimum, maximum);
        }

        public Dictionary<Guid, DummyCombat> BorderClashes { get; private set; }
        public Dictionary<Guid, DummyCombat> MassInvasions { get; private set; }
        public Dictionary<Guid, DummyCombat> Invasions { get; private set; }
        public Dictionary<Guid, DummyCombat> SpoilsOfWar { get; private set; }
        public Random NumberGenerator { get; set; }
        public List<int> NumberGeneratorInjections { get; set; }
    }

    static class ControllerMockWorldRepositoryExtensions
    {
        static public ControllerMockSetupContext SetupBorderClash(this ControllerMockSetupContext setupContext, Guid attackingRegion, UInt32 attackingTroops, Guid secondAttackingRegion, UInt32 secondAttackingTroops)
        {
            SetupBorderClashWithoutPendingInvasions(setupContext, attackingRegion, attackingTroops, secondAttackingRegion, secondAttackingTroops);
            SetupInvasionPendingBorderClash(setupContext, attackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[attackingRegion].TroopCount - attackingTroops);
            SetupInvasionPendingBorderClash(setupContext, secondAttackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[secondAttackingRegion].TroopCount - secondAttackingTroops);
            return setupContext;
        }

        static private ControllerMockSetupContext SetupBorderClashWithoutPendingInvasions(this ControllerMockSetupContext setupContext, Guid attackingRegion, UInt32 attackingTroops, Guid secondAttackingRegion, UInt32 secondAttackingTroops)
        {
            Guid combatId = Guid.NewGuid();
            DummyCombat combat = new DummyCombat(combatId, CombatType.BorderClash);
            combat.SetupAddArmy(attackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[attackingRegion].OwnerId, CombatArmyMode.Attacking, attackingTroops);
            combat.SetupAddArmy(secondAttackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[secondAttackingRegion].OwnerId, CombatArmyMode.Attacking, secondAttackingTroops);
            setupContext.ControllerMock.WorldRepository.BorderClashes[combatId] = combat;
            return setupContext;
        }

        static public ControllerMockSetupContext SetupMassInvasion(this ControllerMockSetupContext setupContext, Guid targetRegion, Guid attackingRegion, UInt32 attackingTroops, Guid secondAttackingRegion, UInt32 secondAttackingTroops)
        {
            Guid combatId = Guid.NewGuid();
            DummyCombat combat = new DummyCombat(combatId, CombatType.MassInvasion);
            combat.SetupAddArmy(attackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[attackingRegion].OwnerId, CombatArmyMode.Attacking, attackingTroops);
            combat.SetupAddArmy(secondAttackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[secondAttackingRegion].OwnerId, CombatArmyMode.Attacking, secondAttackingTroops);
            combat.SetupAddArmy(targetRegion, setupContext.ControllerMock.RegionRepository.RegionData[targetRegion].OwnerId, CombatArmyMode.Defending, setupContext.ControllerMock.RegionRepository.RegionData[targetRegion].TroopCount);
            setupContext.ControllerMock.WorldRepository.MassInvasions[combatId] = combat;
            return setupContext;
        }

        static public ControllerMockSetupContext SetupMassInvasionWithBorderClash(this ControllerMockSetupContext setupContext, Guid targetRegion, UInt32 counterAttackingTroops, Guid attackingRegion, UInt32 attackingTroops, Guid counterAttackedRegion, UInt32 secondAttackingTroops)
        {
            Guid combatId = Guid.NewGuid();
            DummyCombat combat = new DummyCombat(combatId, CombatType.MassInvasion);
            combat.SetupAddArmy(attackingRegion, setupContext.ControllerMock.RegionRepository.RegionData[attackingRegion].OwnerId, CombatArmyMode.Attacking, attackingTroops);
            combat.SetupAddArmy(targetRegion, setupContext.ControllerMock.RegionRepository.RegionData[targetRegion].OwnerId, CombatArmyMode.Defending, setupContext.ControllerMock.RegionRepository.RegionData[targetRegion].TroopCount - counterAttackingTroops);
            setupContext.ControllerMock.WorldRepository.MassInvasions[combatId] = combat;

            SetupBorderClashWithoutPendingInvasions(setupContext, targetRegion, counterAttackingTroops, counterAttackedRegion, secondAttackingTroops);
            SetupInvasionPendingBorderClash(setupContext, counterAttackedRegion, setupContext.ControllerMock.RegionRepository.RegionData[counterAttackedRegion].TroopCount - secondAttackingTroops);
            return setupContext;
        }

        static private ControllerMockSetupContext SetupInvasionPendingBorderClash(this ControllerMockSetupContext setupContext, Guid defendingRegion, UInt32 defendingTroops)
        {
            Guid combatId = Guid.NewGuid();
            DummyCombat combat = new DummyCombat(combatId, CombatType.Invasion);
            combat.SetupAddArmy(defendingRegion, setupContext.ControllerMock.RegionRepository.RegionData[defendingRegion].OwnerId, CombatArmyMode.Defending, defendingTroops);
            setupContext.ControllerMock.WorldRepository.Invasions[combatId] = combat;
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

        static public ICombat GetMassInvasion(this ControllerMock controller, Guid defendingRegion)
        {
            var query = from combatEntry in controller.WorldRepository.MassInvasions
                        let combat = combatEntry.Value
                        let defendingArmy = combat.InvolvedArmies.Where(army => army.ArmyMode == CombatArmyMode.Defending).FirstOrDefault()
                        where defendingArmy.OriginRegionId == defendingRegion
                        select combat;

            return query.FirstOrDefault();
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
