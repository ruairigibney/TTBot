using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace TTBot.Models
{
    [Alias("EventAliasMapping")]
    public class EventAliasMappingModel
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }

        [ForeignKey(typeof(Event))]
        public ulong EventId { get; set; }
        public string Alias { get; set; }

    }
}
