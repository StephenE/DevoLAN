using Peril.Api.Repository;
using Peril.Api.Tests.Controllers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peril.Api.Tests.Repository
{
    class DummyNationRepository : INationRepository
    {
        public DummyNationRepository()
        {
            NationData = new Dictionary<string, DummyNationData>();
        }

        public async Task<INationData> GetNation(string userId)
        {
            if(NationData.ContainsKey(userId))
            {
                return NationData[userId];
            }
            else
            {
                return null;
            }
        }

        #region - Test Setup Helpers -
        internal DummyNationData SetupDummyNation(Guid sessionId, String ownerId)
        {
            DummyNationData nation = new DummyNationData(ownerId);
            NationData[ownerId] = nation;
            return nation;
        }
        #endregion

        public Dictionary<String, DummyNationData> NationData { get; private set; }
    }

    static class ControllerMockNationRepositoryExtensions
    {
        static public ControllerMockSetupContext SetupAvailableReinforcements(this ControllerMockSetupContext setupContext, UInt32 availableReinforcements)
        {
            return SetupAvailableReinforcements(setupContext, setupContext.ControllerMock.OwnerId, availableReinforcements);
        }

        static public ControllerMockSetupContext SetupAvailableReinforcements(this ControllerMockSetupContext setupContext, String userId, UInt32 availableReinforcements)
        {
            setupContext.ControllerMock.NationRepository.NationData[userId].AvailableReinforcements = availableReinforcements;
            return setupContext;
        }
    }
}
