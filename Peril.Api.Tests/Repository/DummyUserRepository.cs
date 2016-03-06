using Moq;
using Peril.Api.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;

namespace Peril.Api.Tests.Repository
{
    class DummyUserRepository : IUserRepository
    {
        public DummyUserRepository()
        {
            var dummyData = from userId in RegisteredUserIds
                                select new ApplicationUser() { Id = userId, UserName = userId };
            var dummyDataQueryable = dummyData.AsQueryable();

            var mockDatabaseSet = new Mock<IDbSet<ApplicationUser>>();
            mockDatabaseSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Provider).Returns(dummyDataQueryable.Provider);
            mockDatabaseSet.As<IQueryable<ApplicationUser>>().Setup(m => m.Expression).Returns(dummyDataQueryable.Expression);
            mockDatabaseSet.As<IQueryable<ApplicationUser>>().Setup(m => m.ElementType).Returns(dummyDataQueryable.ElementType);
            mockDatabaseSet.As<IQueryable<ApplicationUser>>().Setup(m => m.GetEnumerator()).Returns(dummyDataQueryable.GetEnumerator);

            Users = mockDatabaseSet.Object;
        }

        public IDbSet<ApplicationUser> Users { get; set; }

        static public String PrimaryUserId { get { return "DummyUser"; } }

        static public List<String> RegisteredUserIds { get { return registeredUsersIds; } }

        public GenericPrincipal GetPrincipal(String userId)
        {
            GenericIdentity identity = new GenericIdentity(userId, "Dummy");
            identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId));
            return new GenericPrincipal(identity, null);
        }

        static private List<String> registeredUsersIds = new List<String>
        {
            PrimaryUserId,
            "DummyUser2",
            "DummyUser3",
            "DummyUser4",
            "DummyUser5",
            "DummyUser6",
            "DummyUser7",
            "DummyUser8"
        };
    }
}
