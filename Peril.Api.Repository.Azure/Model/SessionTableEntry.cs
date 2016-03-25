using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Model;
using Peril.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peril.Api.Repository.Azure.Model
{
    public class SessionTableEntry : TableEntity, ISessionData
    {
        public SessionTableEntry(String ownerId, Guid sessionId)
        {
            PartitionKey = sessionId.ToString();
            RowKey = ownerId;
            PhaseId = Guid.Empty;
            RawPhaseType = (Int32)SessionPhase.NotStarted;
            m_ColoursInUse = new List<PlayerColour>();
            RawRound = 1;
        }

        public SessionTableEntry()
        {
            m_ColoursInUse = new List<PlayerColour>();
            RawRound = 1;
        }

        public Guid GameId { get { return Guid.Parse(PartitionKey); } }
        public String OwnerId { get { return RowKey; } }
        public String CurrentEtag { get { return ETag; } }
        public SessionPhase PhaseType { get { return (SessionPhase)RawPhaseType; } }
        public UInt32 Round { get { return (UInt32)RawRound; } }

        public Guid PhaseId { get; set; }
        public Int32 RawPhaseType { get; set; }
        public Int32 RawRound { get; set; }
        
        public String ColoursInUse
        {
            get
            {
                return m_ColoursInUseString;
            }
            set
            {
                m_ColoursInUseString = value;
                m_ColoursInUse.Clear();

                if (!String.IsNullOrEmpty(m_ColoursInUseString))
                {
                    String[] colourStrings = m_ColoursInUseString.Split(';');
                    foreach (String colourString in colourStrings)
                    {
                        PlayerColour result;
                        if (Enum.TryParse(colourString, out result))
                        {
                            m_ColoursInUse.Add(result);
                        }
                    }
                }
            }
        }

        public bool IsColourUsed(PlayerColour colour)
        {
            return m_ColoursInUse.Contains(colour);
        }

        internal void AddUsedColour(PlayerColour colour)
        {
            m_ColoursInUse.Add(colour);

            StringBuilder builder = new StringBuilder();
            foreach (PlayerColour usedColour in m_ColoursInUse)
            {
                builder.Append(usedColour.ToString());
                builder.Append(';');
            }

            m_ColoursInUseString = builder.ToString().TrimEnd(';');
        }

        private String m_ColoursInUseString;
        private List<PlayerColour> m_ColoursInUse;
    }
}
