using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Model;
using System;
using System.Diagnostics;

namespace Peril.Api.Repository.Azure.Model
{
    public class CommandQueueTableEntry : TableEntity, IDeployReinforcementsMessage, IOrderAttackMessage, IRedeployMessage
    {
        static public CommandQueueTableEntry CreateReinforceMessage(Guid sessionId, Guid phaseId, Guid targetRegion, String targetRegionEtag, UInt32 numberOfTroops)
        {
            return new CommandQueueTableEntry(sessionId, Guid.NewGuid())
            {
                RawMessageType = (Int32)CommandQueueMessageType.Reinforce,
                PhaseId = phaseId,
                TargetRegion = targetRegion,
                TargetRegionEtag = targetRegionEtag,
                RawNumberOfTroops = (Int32)numberOfTroops
            };
        }

        static public CommandQueueTableEntry CreateAttackMessage(Guid sessionId, Guid phaseId, Guid sourceRegion, String sourceRegionEtag, Guid targetRegion, UInt32 numberOfTroops)
        {
            return new CommandQueueTableEntry(sessionId, Guid.NewGuid())
            {
                RawMessageType = (Int32)CommandQueueMessageType.Attack,
                PhaseId = phaseId,
                SourceRegion = sourceRegion,
                SourceRegionEtag = sourceRegionEtag,
                TargetRegion = targetRegion,
                RawNumberOfTroops = (Int32)numberOfTroops
            };
        }

        static public CommandQueueTableEntry CreateRedeployMessage(Guid sessionId, Guid phaseId, String nationEtag, Guid sourceRegion, Guid targetRegion, UInt32 numberOfTroops)
        {
            return new CommandQueueTableEntry(sessionId, Guid.NewGuid())
            {
                RawMessageType = (Int32)CommandQueueMessageType.Redeploy,
                PhaseId = phaseId,
                SourceRegion = sourceRegion,
                TargetRegion = targetRegion,
                RawNumberOfTroops = (Int32)numberOfTroops
            };
        }

        private CommandQueueTableEntry(Guid sessionId, Guid messageId)
        {
            PartitionKey = sessionId.ToString();
            RowKey = "Command_" + messageId.ToString();
        }

        public CommandQueueTableEntry()
        {

        }

        [Conditional("DEBUG")]
        public void IsValid()
        {
            if (!RowKey.StartsWith("Command_"))
            {
                throw new InvalidOperationException(String.Format("RowKey {0} doesn't start with 'Command_'", RowKey));
            }
        }

        public Guid SessionId { get { return Guid.Parse(PartitionKey); } }
        public Guid OperationId { get { return Guid.Parse(RowKey.Substring(8)); } }
        public CommandQueueMessageType MessageType { get { return (CommandQueueMessageType)RawMessageType; } }
        public UInt32 NumberOfTroops { get { return (UInt32)RawNumberOfTroops; } }

        public Guid PhaseId { get; set; }
        public Guid TargetRegion { get; set; }
        public String TargetRegionEtag { get; set; }
        public Guid SourceRegion { get; set; }
        public String SourceRegionEtag { get; set; }


        public Int32 RawMessageType { get; set; }
        public Int32 RawNumberOfTroops { get; set; }
    }
}
