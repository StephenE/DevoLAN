using Peril.Api.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace Peril.Api.Models
{
    public class UserRepository : IUserRepository
    {
        public UserRepository(ApplicationDbContext database)
        {
            Database = database;
        }

        public IDbSet<ApplicationUser> Users
        {
            get { return Database.Users; }
        }

        private ApplicationDbContext Database { get; set; }
    }
}
