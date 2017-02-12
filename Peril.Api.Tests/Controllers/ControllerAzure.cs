using Peril.Api.Controllers.Api;
using Peril.Api.Repository.Azure;
using Peril.Api.Tests.Repository;
using System;

namespace Peril.Api.Tests
{
    class ControllerAzure
    {
        public ControllerAzure(String developmentStorageAccountConnectionString, String worldDefinitionPath)
            : this(developmentStorageAccountConnectionString, worldDefinitionPath, DummyUserRepository.PrimaryUserId)
        {
        }

        public ControllerAzure(String developmentStorageAccountConnectionString, String worldDefinitionPath, String userId)
        {
            OwnerId = userId;

            UserRepository = new DummyUserRepository();

            AzureCommandQueue = new CommandQueue(developmentStorageAccountConnectionString);
            AzureNationRepository = new NationRepository(developmentStorageAccountConnectionString);
            AzureRegionRepository = new RegionRepository(developmentStorageAccountConnectionString, worldDefinitionPath);
            AzureSessionRepository = new SessionRepository(developmentStorageAccountConnectionString);
            AzureWorldRepository = new WorldRepository(developmentStorageAccountConnectionString);

            CreateControllers();
        }

        public String OwnerId { get; private set; }

        public GameController GameController { get; private set; }
        public NationController NationController { get; private set; }
        public RegionController RegionController { get; private set; }
        public WorldController WorldController { get; private set; }

        public SessionRepository AzureSessionRepository { get; private set; }
        public CommandQueue AzureCommandQueue { get; private set; }
        public RegionRepository AzureRegionRepository { get; private set; }
        public WorldRepository AzureWorldRepository { get; private set; }
        public NationRepository AzureNationRepository { get; private set; }

        public DummyUserRepository UserRepository { get; private set; }

        public GameController CreateGameController(String userId)
        {
            GameController controller = new GameController(AzureCommandQueue, AzureNationRepository, AzureRegionRepository, AzureSessionRepository, UserRepository, AzureWorldRepository);
            controller.ControllerContext.RequestContext.Principal = UserRepository.GetPrincipal(userId);
            return controller;
        }

        public NationController CreateNationController(String userId)
        {
            NationController controller = new NationController(AzureNationRepository, AzureSessionRepository);
            controller.ControllerContext.RequestContext.Principal = controller.ControllerContext.RequestContext.Principal = UserRepository.GetPrincipal(userId);
            return controller;
        }

        public RegionController CreateRegionController(String userId)
        {
            RegionController controller = new RegionController(AzureCommandQueue, AzureNationRepository, AzureRegionRepository, AzureSessionRepository, UserRepository);
            controller.ControllerContext.RequestContext.Principal = controller.ControllerContext.RequestContext.Principal = UserRepository.GetPrincipal(userId);
            return controller;
        }

        public WorldController CreateWorldController(String userId)
        {
            WorldController controller = new WorldController(AzureNationRepository, AzureRegionRepository, AzureSessionRepository, AzureWorldRepository);
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
}
