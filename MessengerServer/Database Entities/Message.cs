using System;
using System.ComponentModel.DataAnnotations;

namespace MessengerServer.Database_Entities
{
    internal class Message
    {
        [Key]
        public int Id { get; set; }

        public User Sender { get; set; }
        public User Receiver { get; set; }
        public string MessageContent { get; set; }
        public DateTime DateOfSendingMessage { get; set; }
    }
}