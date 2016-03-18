using System;

namespace Peril.Api.Repository.Model
{
    public enum CommandQueueMessageType
    {
        Reinforce,
        Attack,
        Redeploy
    }

    public interface ICommandQueueMessage
    {
        Guid OperationId { get; }
        CommandQueueMessageType MessageType { get; }
        Guid SessionId { get; }
        Guid PhaseId { get; }
    }

    public interface IDeployReinforcementsMessage : ICommandQueueMessage
    {
        Guid TargetRegion { get; }
        String TargetRegionEtag { get; }
        UInt32 NumberOfTroops { get; }
    }

    public interface IOrderAttackMessage : ICommandQueueMessage
    {
        Guid SourceRegion { get; }
        String SourceRegionEtag { get; }
        Guid TargetRegion { get; }
        UInt32 NumberOfTroops { get; }
    }

    public interface IRedeployMessage : ICommandQueueMessage
    {
        Guid SourceRegion { get; }
        Guid TargetRegion { get; }
        UInt32 NumberOfTroops { get; }
    }
}
