using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessengerServer.Database_Entities
{
    class Message
    {
        [Key]
        public int Id { get; set; }
        public User Sender { get; set; }
        public User Receiver { get; set; }
        public string MessageContent { get; set; }
        public DateTime DateOfSendingMessage { get; set; }
        public bool IsDelivered { get; set; }
    }
}
