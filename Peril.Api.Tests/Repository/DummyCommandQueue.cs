using Peril.Api.Repository;
using System;
using System.Collections.Generic;
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

        public Task<Guid> DeployReinforcements(Guid phaseId, string nationEtag, Guid targetRegion, uint numberOfTroops)
        {
            Guid operationId = Guid.NewGuid();
            DummyDeployReinforcementsQueue.Add(new DummyDeployReinforcements
            {
                OperationId = operationId,
                PhaseId = phaseId,
                NationEtag = nationEtag,
                TargetRegion = targetRegion,
                NumberOfTroops = numberOfTroops
            });
            return Task.FromResult(operationId);
        }

        public Task<Guid> OrderAttack(Guid phaseId, Guid sourceRegion, string sourceRegionEtag, Guid targetRegion, uint numberOfTroops)
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

        public Task<Guid> Redeploy(Guid phaseId, string nationEtag, Guid sourceRegion, Guid targetRegion, uint numberOfTroops)
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

        public List<DummyDeployReinforcements> DummyDeployReinforcementsQueue { get; set; }
        public List<DummyOrderAttack> DummyOrderAttackQueue { get; set; }
        public List<DummyRedeploy> DummyRedeployQueue { get; set; }
    }

    class DummyDeployReinforcements
    {
        public Guid OperationId { get; set; }
        public Guid PhaseId { get; set; }
        public String NationEtag { get; set; }
        public Guid TargetRegion { get; set; }
        public UInt32 NumberOfTroops { get; set; }
    }

    class DummyOrderAttack
    {
        public Guid OperationId { get; set; }
        public Guid PhaseId { get; set; }
        public Guid SourceRegion { get; set; }
        public String SourceRegionEtag { get; set; }
        public Guid TargetRegion { get; set; }
        public UInt32 NumberOfTroops { get; set; }
    }

    class DummyRedeploy
    {
        public Guid OperationId { get; set; }
        public Guid PhaseId { get; set; }
        public Guid SourceRegion { get; set; }
        public Guid TargetRegion { get; set; }
        public UInt32 NumberOfTroops { get; set; }
    }
}
