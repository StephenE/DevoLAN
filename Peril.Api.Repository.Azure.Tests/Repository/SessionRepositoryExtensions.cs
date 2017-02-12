using Microsoft.WindowsAzure.Storage.Table;
using Peril.Api.Repository.Azure.Model;
using Peril.Core;
using System;
using System.Threading.Tasks;

namespace Peril.Api.Repository.Azure.Tests.Repository
{
    static class SessionRepositoryExtensions
    {
        static internal CloudTable SetupSessionDataTable(this CloudTableClient tableClient, Guid sessionId)
        {
            var dataTable = SessionRepository.GetTableForSessionData(tableClient, sessionId);
            dataTable.DeleteIfExists();
            dataTable.Create();
            return dataTable;
        }

        static internal async Task<SessionTableEntry> SetupSession(this SessionRepository repository, Guid sessionId, String ownerId)
        {
            SessionTableEntry newSession = new SessionTableEntry(ownerId, sessionId);
            TableOperation insertOperation = TableOperation.InsertOrReplace(newSession);
            await repository.SessionTable.ExecuteAsync(insertOperation);

            var dataTable = repository.GetTableForSessionData(sessionId);
            dataTable.DeleteIfExists();
            dataTable.Create();

            await repository.SetupAddPlayer(sessionId, ownerId);

            return newSession;
        }

        static internal async Task<NationTableEntry> SetupAddPlayer(this SessionRepository repository, Guid sessionId, String userId)
        {
            var dataTable = repository.GetTableForSessionData(sessionId);

            NationTableEntry newSessionPlayerEntry = new NationTableEntry(sessionId, userId);

            TableOperation insertOperation = TableOperation.InsertOrReplace(newSessionPlayerEntry);
            await dataTable.ExecuteAsync(insertOperation);

            return newSessionPlayerEntry;
        }

        static internal async Task<RegionTableEntry> SetupAddRegion(this SessionRepository repository, Guid sessionId, Guid regionId, Guid continentId, String regionName)
        {
            var dataTable = repository.GetTableForSessionData(sessionId);

            RegionTableEntry newRegionEntry = new RegionTableEntry(sessionId, regionId, continentId, regionName);

            TableOperation insertOperation = TableOperation.InsertOrReplace(newRegionEntry);
            await dataTable.ExecuteAsync(insertOperation);

            return newRegionEntry;
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
