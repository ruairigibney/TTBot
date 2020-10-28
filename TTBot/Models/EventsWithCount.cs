using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace TTBot.Models
{
    [Alias("EventsWithCount")]
    public class EventsWithCount : Event
    {
        public int ParticipantCount { get; set; }
    }
}
