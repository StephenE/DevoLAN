using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Peril.Api.Repository.Azure.Model
{
    public class RegionTableEntry : TableEntity, IRegionData
    {
        public RegionTableEntry(Guid sessionId, Guid regionId, Guid continentId, String name, UInt32 cardValue)
        {
            PartitionKey = sessionId.ToString();
            RowKey = "Region_" + regionId.ToString();
            ContinentId = continentId;
            Name = name;
            OwnerId = String.Empty;
            StoredTroopCount = 0;
            StoredTroopsCommittedToPhase = 0;
            StoredCardValue = (Int32)cardValue;
            m_ConnectedRegionList = new List<Guid>();
        }

        public RegionTableEntry()
        {
            m_ConnectedRegionList = new List<Guid>();
        }

        [Conditional("DEBUG")]
        public void IsValid()
        {
            if (!RowKey.StartsWith("Region_"))
            {
                throw new InvalidOperationException(String.Format("RowKey {0} doesn't start with 'Region_'", RowKey));
            }
        }

        public Guid SessionId
        {
            get { return Guid.Parse(PartitionKey); }
        }

        public Guid RegionId
        {
            get { return Guid.Parse(RowKey.Substring(7)); }
        }

        public Guid ContinentId { get; set; }

        public String Name { get; set; }

        public IEnumerable<Guid> ConnectedRegions
        {
            get { return m_ConnectedRegionList; }
        }

        public String OwnerId { get; set; }

        public Int32 StoredTroopCount { get; set; }

        public Int32 StoredCardValue { get; set; }

        public Int32 StoredTroopsCommittedToPhase { get; set; }

        public String ConnectedRegionString
        {
            get
            {
                return m_ConnectedRegionString;
            }
            set
            {
                m_ConnectedRegionString = value;
                m_ConnectedRegionList.Clear();

                if (!String.IsNullOrEmpty(m_ConnectedRegionString))
                {
                    String[] guidStrings = m_ConnectedRegionString.Split(';');
                    foreach(String guidString in guidStrings)
                    {
                        Guid result;
                        if(Guid.TryParse(guidString, out result))
                        {
                            m_ConnectedRegionList.Add(result);
                        }
                    }
                }
            }
        }

        public UInt32 TroopsCommittedToPhase { get { return (UInt32)StoredTroopsCommittedToPhase; } }

        public string CurrentEtag { get { return ETag; } }

        public UInt32 TroopCount { get { return (UInt32)StoredTroopCount; } }

        public UInt32 CardValue { get { return (UInt32)StoredCardValue; } }

        internal void SetConnectedRegions(IEnumerable<Guid> regions)
        {
            m_ConnectedRegionList = regions.ToList();

            StringBuilder builder = new StringBuilder();
            foreach(Guid regionId in m_ConnectedRegionList)
            {
                builder.Append(regionId.ToString());
                builder.Append(';');
            }

            m_ConnectedRegionString = builder.ToString().TrimEnd(';');
        }

        private List<Guid> m_ConnectedRegionList;
        private String m_ConnectedRegionString;
    }
}
