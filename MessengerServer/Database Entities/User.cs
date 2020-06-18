using System.ComponentModel.DataAnnotations;

namespace MessengerServer.Database_Entities
{
    internal class User
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }
    }
}