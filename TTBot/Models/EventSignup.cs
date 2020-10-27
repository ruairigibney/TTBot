using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace TTBot.Models
{
    public class EventSignup
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }

        [ForeignKey(typeof(Event))]
        public int EventId { get; set; }
        public string UserId { get; set; }
    }
}
