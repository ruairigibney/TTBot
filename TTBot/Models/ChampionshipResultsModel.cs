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
        public string Driver { get; set; }

        [ForeignKey(typeof(Championship))]
        public int ChampionshipId { get; set; }
        public string[] Positions { get; set; }
        public int Points { get; set; }

    }
}
