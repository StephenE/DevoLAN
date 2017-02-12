using Microsoft.WindowsAzure.Storage.Table;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Peril.Api.Repository.Azure.Model
{
    public class CombatResultTableEntry : TableEntity, ICombatResult
    {
        public CombatResultTableEntry(Guid sessionId, Guid combatId, IEnumerable<ICombatRoundResult> results)
        {
            PartitionKey = sessionId.ToString();
            RowKey = "Result_" + combatId.ToString();
            CombatId = combatId;
            SetResults(results);
        }

        public CombatResultTableEntry()
        {
            m_CombatRoundResults = new List<ICombatRoundResult>();
        }

        [Conditional("DEBUG")]
        public void IsValid()
        {
            if (!RowKey.StartsWith("Result_"))
            {
                throw new InvalidOperationException(String.Format("RowKey {0} doesn't start with 'Result_'", RowKey));
            }
        }

        public Guid SessionId { get { return Guid.Parse(PartitionKey); } }
        public IEnumerable<ICombatRoundResult> Rounds { get { return m_CombatRoundResults; } }

        public Guid CombatId { get; set; }

        public String CombatResultString
        {
            get
            {
                return m_CombatRoundResultsString;
            }
            set
            {
                m_CombatRoundResultsString = value;
                m_CombatRoundResults.Clear();

                if (!String.IsNullOrEmpty(m_CombatRoundResultsString))
                {
                    String[] roundStrings = m_CombatRoundResultsString.Split(';');
                    foreach (String roundString in roundStrings)
                    {
                        m_CombatRoundResults.Add(CombatRoundResult.CreateFromAzureString(roundString));
                    }
                }
            }
        }

        public void SetResults(IEnumerable<ICombatRoundResult> results)
        {
            m_CombatRoundResults = results.ToList();

            StringBuilder builder = new StringBuilder();
            foreach (ICombatRoundResult result in results)
            {
                builder.Append(result.EncodeToAzureString());
                builder.Append(';');
            }

            m_CombatRoundResultsString = builder.ToString().TrimEnd(';');
        }

        private List<ICombatRoundResult> m_CombatRoundResults;
        private String m_CombatRoundResultsString;
    }
}
