using Moq;
using Peril.Api.Repository;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Peril.Api.Tests.Repository
{
    class DummyUserRepository : IUserRepository
    {
        public DummyUserRepository()
        {
            var dummyData = new List<ApplicationUser>
            {
                new ApplicationUser() { Id = "DummyUser", UserName = "DummyUser" }
            }.AsQueryable();

            var mockDatabaseSet = new Mock<IDbSet<ApplicationUser>>();
            mockDatabaseSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Provider).Returns(dummyData.Provider);
            mockDatabaseSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Expression).Returns(dummyData.Expression);
            mockDatabaseSet.As<IQueryable<ApplicationUser>>().Setup(m => m.ElementType).Returns(dummyData.ElementType);
            mockDatabaseSet.As<IQueryable<ApplicationUser>>().Setup(m => m.GetEnumerator()).Returns(dummyData.GetEnumerator);

            Users = mockDatabaseSet.Object;
        }

        public IDbSet<ApplicationUser> Users { get; set; }
    }
}
