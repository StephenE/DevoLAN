using Peril.Api.Repository;
using Peril.Api.Repository.Model;
using Peril.Api.Tests.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peril.Api.Tests.Repository
{
    class DummyCommandQueue : ICommandQueue
    {
        public DummyCommandQueue()
        {
            DummyDeployReinforcementsQueue = new List<DummyDeployReinforcements>();
            DummyOrderAttackQueue = new List<DummyOrderAttack>();
            DummyRedeployQueue = new List<DummyRedeploy>();
        }

        public Task<Guid> DeployReinforcements(Guid sessionId, Guid phaseId, Guid targetRegion, String regionEtag, UInt32 numberOfTroops)
        {
            Guid operationId = Guid.NewGuid();
            DummyDeployReinforcementsQueue.Add(new DummyDeployReinforcements
            {
                OperationId = operationId,
                PhaseId = phaseId,
                TargetRegion = targetRegion,
                TargetRegionEtag = regionEtag,
                NumberOfTroops = numberOfTroops
            });
            return Task.FromResult(operationId);
        }

        public Task<Guid> OrderAttack(Guid sessionId, Guid phaseId, Guid sourceRegion, String sourceRegionEtag, Guid targetRegion, UInt32 numberOfTroops)
        {
            Guid operationId = Guid.NewGuid();
            DummyOrderAttackQueue.Add(new DummyOrderAttack
            {
                OperationId = operationId,
                PhaseId = phaseId,
                SourceRegion = sourceRegion,
                SourceRegionEtag = sourceRegionEtag,
                TargetRegion = targetRegion,
                NumberOfTroops = numberOfTroops
            });
            return Task.FromResult(operationId);
        }

        public Task<Guid> Redeploy(Guid sessionId, Guid phaseId, String nationEtag, Guid sourceRegion, Guid targetRegion, UInt32 numberOfTroops)
        {
            Guid operationId = Guid.NewGuid();
            DummyRedeployQueue.Add(new DummyRedeploy
            {
                OperationId = operationId,
                PhaseId = phaseId,
                SourceRegion = sourceRegion,
                TargetRegion = targetRegion,
                NumberOfTroops = numberOfTroops
            });
            return Task.FromResult(operationId);
        }

        public Task<IEnumerable<ICommandQueueMessage>> GetQueuedCommands(Guid sessionId, Guid sessionPhaseId)
        {
            List<ICommandQueueMessage> messages = new List<ICommandQueueMessage>();
            messages.AddRange(DummyDeployReinforcementsQueue);
            messages.AddRange(DummyOrderAttackQueue);
            messages.AddRange(DummyRedeployQueue);

            return Task.FromResult<IEnumerable<ICommandQueueMessage>>(from message in messages
                                                                      where message.PhaseId == sessionPhaseId
                                                                      select message);
        }

        public Task RemoveCommands(Guid sessionPhaseId)
        {
            DummyDeployReinforcementsQueue.RemoveAll(message => message.PhaseId == sessionPhaseId);
            DummyOrderAttackQueue.RemoveAll(message => message.PhaseId == sessionPhaseId);
            DummyRedeployQueue.RemoveAll(message => message.PhaseId == sessionPhaseId);
            return Task.FromResult(false);
        }

        public List<DummyDeployReinforcements> DummyDeployReinforcementsQueue { get; set; }
        public List<DummyOrderAttack> DummyOrderAttackQueue { get; set; }
        public List<DummyRedeploy> DummyRedeployQueue { get; set; }
    }

    class DummyDeployReinforcements : IDeployReinforcementsMessage
    {
        public CommandQueueMessageType MessageType { get { return CommandQueueMessageType.Reinforce; } }
        public Guid OperationId { get; set; }
        public Guid SessionId { get; set; }
        public Guid PhaseId { get; set; }
        public Guid TargetRegion { get; set; }
        public String TargetRegionEtag { get; set; }
        public UInt32 NumberOfTroops { get; set; }
    }

    class DummyOrderAttack : IOrderAttackMessage
    {
        public CommandQueueMessageType MessageType { get { return CommandQueueMessageType.Attack; } }
        public Guid OperationId { get; set; }
        public Guid SessionId { get; set; }
        public Guid PhaseId { get; set; }
        public Guid SourceRegion { get; set; }
        public String SourceRegionEtag { get; set; }
        public Guid TargetRegion { get; set; }
        public UInt32 NumberOfTroops { get; set; }
    }

    class DummyRedeploy : IRedeployMessage
    {
        public CommandQueueMessageType MessageType { get { return CommandQueueMessageType.Redeploy; } }
        public Guid SessionId { get; set; }
        public Guid OperationId { get; set; }
        public Guid PhaseId { get; set; }
        public Guid SourceRegion { get; set; }
        public Guid TargetRegion { get; set; }
        public UInt32 NumberOfTroops { get; set; }
    }

    static class ControllerMockCommandQueueExtensions
    {
        static public ControllerMockSetupContext QueueDeployReinforcements(this ControllerMockSetupContext setupContext, Guid regionId, UInt32 numberOfTroops)
        {
            setupContext.ControllerMock.CommandQueue.DummyDeployReinforcementsQueue.Add(new DummyDeployReinforcements
            {
                OperationId = Guid.NewGuid(),
                SessionId = setupContext.DummySession.GameId,
                PhaseId = setupContext.DummySession.PhaseId,
                TargetRegion = regionId,
                TargetRegionEtag = setupContext.ControllerMock.RegionRepository.RegionData[regionId].CurrentEtag,
                NumberOfTroops = numberOfTroops
            });
            return setupContext;
        }

        static public ControllerMockSetupContext QueueAttack(this ControllerMockSetupContext setupContext, Guid sourceRegionId, Guid targetRegionId, UInt32 numberOfTroops)
        {
            Guid operationId;
            return QueueAttack(setupContext, sourceRegionId, targetRegionId, numberOfTroops, out operationId);
        }

        static public ControllerMockSetupContext QueueAttack(this ControllerMockSetupContext setupContext, Guid sourceRegionId, Guid targetRegionId, UInt32 numberOfTroops, out Guid operationId)
        {
            operationId = Guid.NewGuid();
            setupContext.ControllerMock.CommandQueue.DummyOrderAttackQueue.Add(new DummyOrderAttack
            {
                OperationId = operationId,
                SessionId = setupContext.DummySession.GameId,
                PhaseId = setupContext.DummySession.PhaseId,
                SourceRegion = sourceRegionId,
                SourceRegionEtag = setupContext.ControllerMock.RegionRepository.RegionData[sourceRegionId].CurrentEtag,
                TargetRegion = targetRegionId,
                NumberOfTroops = numberOfTroops
            });
            return setupContext;
        }
    }
}
