using System;

namespace Peril.Core
{
    public enum SessionPhase
    {
        NotStarted,
        Reinforcements,
        CombatOrders,
        BorderClashes,
        MassInvasions,
        Invasions,
        SpoilsOfWar,
        Redeployment,
        Victory
    };

    public interface ISession
    {
        Guid GameId { get; }

        Guid PhaseId { get; }

        UInt32 Round { get; }

        String OwnerId { get; }

        SessionPhase PhaseType { get; }
    }
}
