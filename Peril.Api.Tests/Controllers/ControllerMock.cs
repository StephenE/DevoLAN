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

            UserRepository = new DummyUserRepository();
            SessionRepository = new DummySessionRepository();

            GameController = CreateGameController(userId);
            RegionController = CreateRegionController(userId);
        }

        public ControllerMock(String userId, ControllerMock linkedData)
        {
            OwnerId = userId;

            UserRepository = linkedData.UserRepository;
            SessionRepository = linkedData.SessionRepository;

            GameController = CreateGameController(userId);
            RegionController = CreateRegionController(userId);
        }

        public String OwnerId { get; private set; }

        public GameController GameController { get; private set; }
        public RegionController RegionController { get; private set; }

        public DummyUserRepository UserRepository { get; private set; }
        public DummySessionRepository SessionRepository { get; private set; }

        private GameController CreateGameController(String userId)
        {
            GameController controller = new GameController(SessionRepository, UserRepository);
            controller.ControllerContext.RequestContext.Principal = UserRepository.GetPrincipal(userId);
            return controller;
        }

        public RegionController CreateRegionController(String userId)
        {
            RegionController controller = new RegionController();
            controller.ControllerContext.RequestContext.Principal = controller.ControllerContext.RequestContext.Principal = UserRepository.GetPrincipal(userId);
            return controller;
        }
    }
}
