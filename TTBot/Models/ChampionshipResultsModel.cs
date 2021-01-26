using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace TTBot.Models
{
    [Alias("ChampionshipResults")]
    public class ChampionshipResultsModel
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }

        [ForeignKey(typeof(Event))]
        public int EventId { get; set; }
        public int Pos { get; set; }
        public string Driver { get; set; }
        public string Number { get; set; }
        public string Car { get; set; }
        public string Points { get; set; }
        public string Diff { get; set; }

    }
}
