using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peril.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace Peril.Api.Repository.Azure.Model
{
    public class CombatTableEntry : TableEntity, ICombat
    {
        public CombatTableEntry(Guid sessionId, Guid combatId, CombatType type)
        {
            PartitionKey = sessionId.ToString();
            RowKey = combatId.ToString();
            ResolutionTypeRaw = (Int32)type;
            m_CombatArmiesList = new List<ICombatArmy>();
        }

        public CombatTableEntry()
        {
            m_CombatArmiesList = new List<ICombatArmy>();
        }

        public Guid SessionId { get { return Guid.Parse(PartitionKey); } }
        public Guid CombatId { get { return Guid.Parse(RowKey); } }
        public CombatType ResolutionType { get { return (CombatType)ResolutionTypeRaw; } }
        public IEnumerable<ICombatArmy> InvolvedArmies { get { return m_CombatArmiesList; } }

        public Int32 ResolutionTypeRaw { get; set; }

        public String CombatArmiesString
        {
            get
            {
                return m_CombatArmiesString;
            }
            set
            {
                m_CombatArmiesString = value;
                m_CombatArmiesList.Clear();

                if (!String.IsNullOrEmpty(m_CombatArmiesString))
                {
                    String[] armyStrings = m_CombatArmiesString.Split(';');
                    foreach (String armyString in armyStrings)
                    {
                        m_CombatArmiesList.Add(CombatArmy.CreateFromAzureString(armyString));
                    }
                }
            }
        }

        public void SetCombatArmy(IEnumerable<ICombatArmy> armies)
        {
            m_CombatArmiesList = armies.ToList();

            StringBuilder builder = new StringBuilder();
            foreach (ICombatArmy army in m_CombatArmiesList)
            {
                builder.Append(army.EncodeToAzureString());
                builder.Append(';');
            }

            m_CombatArmiesString = builder.ToString().TrimEnd(';');
        }

        private List<ICombatArmy> m_CombatArmiesList;
        private String m_CombatArmiesString;
    }
}
