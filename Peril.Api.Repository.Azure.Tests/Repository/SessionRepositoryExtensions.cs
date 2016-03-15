using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Core;
using System;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure.Tests.Repository
{
    static class SessionRepositoryExtensions
    {
        static internal async Task<SessionTableEntry> SetupSession(this SessionRepository repository, Guid sessionId, String ownerId)
        {
            SessionTableEntry newSession = new SessionTableEntry(ownerId, sessionId);
            TableOperation insertOperation = TableOperation.InsertOrReplace(newSession);
            await repository.SessionTable.ExecuteAsync(insertOperation);

            await repository.SetupAddPlayer(sessionId, ownerId);

            return newSession;
        }

        static internal async Task<NationTableEntry> SetupAddPlayer(this SessionRepository repository, Guid sessionId, String userId)
        {
            NationTableEntry newSessionPlayerEntry = new NationTableEntry(sessionId, userId);

            TableOperation insertOperation = TableOperation.InsertOrReplace(newSessionPlayerEntry);
            await repository.SessionPlayersTable.ExecuteAsync(insertOperation);

            return newSessionPlayerEntry;
        }

        static internal async Task<SessionTableEntry> SetupSessionPhase(this Task<SessionTableEntry> sessionTableEntryTask, SessionRepository repository, SessionPhase round)
        {
            var sessionTableEntry = await sessionTableEntryTask;
            sessionTableEntry.RawPhaseType = (Int32)round;
            sessionTableEntry.PhaseId = Guid.NewGuid();

            TableOperation insertOperation = TableOperation.Replace(sessionTableEntry);
            await repository.SessionTable.ExecuteAsync(insertOperation);

            return sessionTableEntry;
        }
    }
}
