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

        SessionPhase PhaseType { get; }
    }
}
