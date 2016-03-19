using Peril.Core;
using System;
using System.Collections.Generic;

namespace Peril.Api.Models
{
    public class Combat : ICombat
    {
        public Combat(ICombat combat)
        {
            CombatId = combat.CombatId;
            ResolutionType = combat.ResolutionType;
            m_InvolvedArmies = new List<CombatArmy>();
            foreach(ICombatArmy army in combat.InvolvedArmies)
            {
                m_InvolvedArmies.Add(new CombatArmy(army));
            }
        }

        public Guid CombatId { get; set; }

        public CombatType ResolutionType { get; set; }

        public IEnumerable<ICombatArmy> InvolvedArmies { get { return m_InvolvedArmies; } }

        private List<CombatArmy> m_InvolvedArmies;
    }
}