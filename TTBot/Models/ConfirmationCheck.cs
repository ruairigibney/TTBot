using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace TTBot.Models
{
    public class ConfirmationCheck
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }
        public string MessageId { get; set; }
        [ForeignKey(typeof(Event))]
        public int EventId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
