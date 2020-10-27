using Dapper;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.DataAccess
{
    public class LeaderboardEntries : ILeaderboardEntries
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public LeaderboardEntries(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task AddAsync(int leaderboardId, TimeSpan time, ulong userId, string proofUrl)
        {
            var entry = new LeaderboardEntry()
            {
                LeaderboardId = leaderboardId,
                Time = time,
                SubmittedById = userId.ToString(),
                ProofUrl = proofUrl,
                SubmittedDate = DateTime.Now
            };

            using (var connection = _connectionFactory.Open())
            {
                await connection.InsertAsync(entry);
            }
        }

        public async Task<LeaderboardEntry> GetBestEntryForUser(int leaderboardId, ulong userId)
        {
            using (var connection = _connectionFactory.Open())
            {
                return (await connection.QueryAsync<LeaderboardEntry>(@" SELECT Id,LeaderboardId,SubmittedById,SubmittedDate,ProofUrl,Time FROM (
	                                                                    SELECT ROW_NUMBER() OVER (PARTITION BY  SubmittedById ORDER BY [Time] ASC) as RowNumber, * 
		                                                                FROM LeaderboardEntries
		                                                                WHERE LeaderboardId = @leaderboardId
                                                                        AND Invalidated = 0
	                                                                    ) X
                                                                    where RowNumber = 1
                                                                    AND SubmittedById = @userId", new { userId = userId.ToString(), leaderboardId = leaderboardId })).FirstOrDefault();
            }
        }

        public async Task Update(LeaderboardEntry entry)
        {
            using (var connection = _connectionFactory.Open())
            {
                await connection.UpdateAsync(entry);
            }
        }
    }
}
