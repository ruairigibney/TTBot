
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace TTBot.Models
{
    [Alias("leaderboardEntries")]
    public class LeaderboardEntry
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }

        [ForeignKey(typeof(Leaderboard))]
        public int LeaderboardId { get; set; }
        public string SubmittedById { get; set; }
        public string ProofUrl { get; set; }
        public TimeSpan Time { get; set; }
        public DateTime SubmittedDate { get; set; }
        public bool Invalidated { get; set; }
    }
}
