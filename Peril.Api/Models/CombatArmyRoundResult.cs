using Peril.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peril.Api.Models
{
    public class CombatArmyRoundResult : ICombatArmyRoundResult
    {
        public CombatArmyRoundResult(ICombatArmyRoundResult result)
        {
            OriginRegionId = result.OriginRegionId;
            OwnerUserId = result.OwnerUserId;
            TroopsLost = result.TroopsLost;
            m_RolledResults = result.RolledResults.ToList();
        }

        private CombatArmyRoundResult(Guid originRegionId, String ownerId)
        {
            OriginRegionId = originRegionId;
            OwnerUserId = ownerId;
            TroopsLost = 0;
            m_RolledResults = new List<UInt32>();
        }

        public Guid OriginRegionId { get; set; }

        public String OwnerUserId { get; set; }

        public IEnumerable<UInt32> RolledResults { get { return m_RolledResults; } }

        public UInt32 TroopsLost { get; set; }

        static public CombatArmyRoundResult GenerateForCombat(CombatArmy army, IEnumerable<UInt32> randomNumberGenerator)
        {
            CombatArmyRoundResult armyRoundResult = new CombatArmyRoundResult(army.OriginRegionId, army.OwnerUserId);

            int maximumDicePoolSize = (army.ArmyMode == CombatArmyMode.Attacking) ? 3 : 2;
            int dicePoolSize = (maximumDicePoolSize < army.NumberOfTroops) ? maximumDicePoolSize : (int)army.NumberOfTroops;

            armyRoundResult.m_RolledResults.AddRange(randomNumberGenerator.Take(dicePoolSize));

            return armyRoundResult;
        }

        private List<UInt32> m_RolledResults;
    }
}
