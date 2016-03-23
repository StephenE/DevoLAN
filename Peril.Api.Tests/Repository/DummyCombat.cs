using Peril.Core;
using System;
using System.Collections.Generic;

namespace Peril.Api.Tests.Repository
{
    public class DummyCombat : ICombat
    {
        public DummyCombat(Guid combatId, CombatType type)
        {
            CombatId = combatId;
            ResolutionType = type;
            m_InvolvedArmies = new List<DummyCombatArmy>();
        }

        public Guid CombatId { get; set; }

        public CombatType ResolutionType { get; set; }

        public IEnumerable<ICombatArmy> InvolvedArmies { get { return m_InvolvedArmies; } }

        public List<DummyCombatArmy> m_InvolvedArmies;

        #region - Test Setup Helpers -
        public void SetupAddArmy(Guid originRegion, String ownerId, CombatArmyMode mode, UInt32 numberOfTroops)
        {
            m_InvolvedArmies.Add(new DummyCombatArmy(originRegion, ownerId, mode, numberOfTroops));
        }
        #endregion
    }
}
