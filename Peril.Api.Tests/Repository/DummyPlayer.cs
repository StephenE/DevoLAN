using Peril.Core;

namespace Peril.Api.Tests.Repository
{
    class DummyPlayer : IPlayer
    {
        public string UserId { get; set; }
        public string Name { get; set; }
    }
}
