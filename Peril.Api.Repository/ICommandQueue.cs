using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peril.Api.Repository
{
    public enum CommandQueueMessageType
    {
        Reinforce,
        Attack,
        Redeploy
    }

    public interface ICommandQueueMessage
    {
        CommandQueueMessageType MessageType { get; }
        Guid SessionId { get; }
        Guid PhaseId { get; }
    }

    public interface IDeployReinforcementsMessage : ICommandQueueMessage
    {
        String NationEtag { get; }
        Guid TargetRegion { get; }
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

    public interface ICommandQueue
    {
        Task<Guid> DeployReinforcements(Guid sessionId, Guid phaseId, String nationEtag, Guid targetRegion, UInt32 numberOfTroops);

        Task<Guid> OrderAttack(Guid sessionId, Guid phaseId, Guid sourceRegion, String sourceRegionEtag, Guid targetRegion, UInt32 numberOfTroops);

        Task<Guid> Redeploy(Guid sessionId, Guid phaseId, String nationEtag, Guid sourceRegion, Guid targetRegion, UInt32 numberOfTroops);

        Task<ICommandQueueMessage> GetQueuedCommands(Guid sessionId);
    }
}
