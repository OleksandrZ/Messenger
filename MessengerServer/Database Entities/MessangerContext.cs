using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessengerServer.Database_Entities
{
    class MessangerContext : DbContext
    {
        public MessangerContext() : base("Messanger")
        { }
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
    }
}
