using Peril.Api.Controllers.Api;
using Peril.Api.Tests.Repository;
using System;

namespace Peril.Api.Tests.Controllers
{
    class ControllerMock
    {
        public ControllerMock()
            : this(DummyUserRepository.PrimaryUserId)
        {
        }

        public ControllerMock(String userId)
        {
            OwnerId = userId;

            CommandQueue = new DummyCommandQueue();
            NationRepository = new DummyNationRepository();
            RegionRepository = new DummyRegionRepository();
            SessionRepository = new DummySessionRepository();
            UserRepository = new DummyUserRepository();

            CreateControllers();
        }

        public ControllerMock(String userId, ControllerMock linkedData)
        {
            OwnerId = userId;

            CommandQueue = linkedData.CommandQueue;
            NationRepository = linkedData.NationRepository;
            RegionRepository = linkedData.RegionRepository;
            SessionRepository = linkedData.SessionRepository;
            UserRepository = linkedData.UserRepository;

            CreateControllers();
        }

        public String OwnerId { get; private set; }

        public GameController GameController { get; private set; }
        public NationController NationController { get; private set; }
        public RegionController RegionController { get; private set; }
        public WorldController WorldController { get; private set; }

        public DummyCommandQueue CommandQueue { get; private set; }
        public DummyNationRepository NationRepository { get; private set; }
        public DummyRegionRepository RegionRepository { get; private set; }
        public DummySessionRepository SessionRepository { get; private set; }
        public DummyUserRepository UserRepository { get; private set; }

        public GameController CreateGameController(String userId)
        {
            GameController controller = new GameController(RegionRepository, SessionRepository, UserRepository);
            controller.ControllerContext.RequestContext.Principal = UserRepository.GetPrincipal(userId);
            return controller;
        }

        public NationController CreateNationController(String userId)
        {
            NationController controller = new NationController();
            controller.ControllerContext.RequestContext.Principal = controller.ControllerContext.RequestContext.Principal = UserRepository.GetPrincipal(userId);
            return controller;
        }

        public RegionController CreateRegionController(String userId)
        {
            RegionController controller = new RegionController(CommandQueue, NationRepository, RegionRepository, SessionRepository, UserRepository);
            controller.ControllerContext.RequestContext.Principal = controller.ControllerContext.RequestContext.Principal = UserRepository.GetPrincipal(userId);
            return controller;
        }

        public WorldController CreateWorldController(String userId)
        {
            WorldController controller = new WorldController();
            controller.ControllerContext.RequestContext.Principal = controller.ControllerContext.RequestContext.Principal = UserRepository.GetPrincipal(userId);
            return controller;
        }

        private void CreateControllers()
        {
            GameController = CreateGameController(OwnerId);
            NationController = CreateNationController(OwnerId);
            RegionController = CreateRegionController(OwnerId);
            WorldController = CreateWorldController(OwnerId);
        }
    }

    class ControllerMockSetupContext
    {
        public ControllerMock ControllerMock { get; set; }
        public DummySession DummySession { get; set; }
        public DummyRegionData DummyRegion { get; set; }
    }
}
