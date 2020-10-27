using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace TTBot.Models
{
    public class Event
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string GuildId { get; set; }
        public string ChannelId { get; set; }
        public bool Closed { get; set; }
    }
}
