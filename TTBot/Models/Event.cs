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
        public string ShortName { get; set; }
        public string GuildId { get; set; }
        public string ChannelId { get; set; }
        public string RoleId { get; set; }
        public bool Closed { get; set; }
        public int? Capacity { get; set; }
        [Ignore]
        public bool SpaceLimited => Capacity.HasValue;
        [Ignore]
        public string DisplayName => ShortName ?? Name;
        public string MessageId { get; set; }
        public int? Round { get; set; }
        public string LastRoundDate { get; set; }
        public string LastRoundTrack { get; set; }
    }
}
