using System.Data.Entity;

namespace MessengerServer.Database_Entities
{
    internal class MessangerContext : DbContext
    {
        public MessangerContext() : base("Messanger")
        { }

        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
    }
}