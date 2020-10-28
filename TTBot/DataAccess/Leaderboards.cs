using Dapper;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTBot.Models;
using TTBot.Services;

namespace TTBot.DataAccess
{
    public class Leaderboards : ILeaderboards
    {
        private readonly IDbConnectionFactory _conFactory;

        public Leaderboards(IDbConnectionFactory conFactory)
        {
            _conFactory = conFactory;
        }

        public async Task<IEnumerable<Leaderboard>> GetAllAsync(ulong guildId)
        {
            using (var connection = _conFactory.Open())
            {
                return await connection.SelectAsync<Leaderboard>(l => l.GuildId == guildId.ToString());
            }
        }

        public async Task<IEnumerable<Leaderboard>> GetAllActiveAsync(ulong guildId)
        {
            using (var connection = _conFactory.Open())
            {
                return await connection.SelectAsync<Leaderboard>(l => l.GuildId == guildId.ToString() && l.Active);
            }
        }

        public async Task AddAsync(ulong guildId, ulong channelId, string game, DateTime? endDate = null, bool active = true)
        {
            using (var connection = _conFactory.Open())
            {
                await connection.InsertAsync(new Leaderboard()
                {
                    GuildId = guildId.ToString(),
                    Active = active,
                    ChannelId = channelId.ToString(),
                    Game = game,
                    StartDateTime = DateTime.Now,
                    EndDateTime = endDate
                });
            }
        }

        public async Task<Leaderboard> GetActiveLeaderboardForChannelAsync(ulong guildId, ulong channelId)
        {
            using (var connection = _conFactory.Open())
            {
                return await connection.SingleAsync<Leaderboard>(l => l.GuildId == guildId.ToString() && l.ChannelId == channelId.ToString() && l.Active);
            }
        }

        public async Task UpdateAsync(Leaderboard leaderboard)
        {
            using (var connection = _conFactory.Open())
            {
                await connection.UpdateAsync(leaderboard);
            }
        }

        public async Task<IEnumerable<LeaderboardEntry>> GetStandingsAsync(int leaderboardId)
        {
            using (var con = _conFactory.Open())
            {
                return await con.QueryAsync<LeaderboardEntry>(@"   SELECT Id,LeaderboardId,SubmittedById,SubmittedDate,ProofUrl,Time FROM (
	                                                                    SELECT ROW_NUMBER() OVER (PARTITION BY  SubmittedById ORDER BY [Time] ASC) as RowNumber, * 
		                                                                FROM LeaderboardEntries
		                                                                WHERE LeaderboardId = @leaderboardId
                                                                        AND Invalidated = 0
	                                                                    ) X
                                                                    where RowNumber = 1
                                                                    ORDER BY [Time] ASC",
                                                        new { leaderboardId });
            }
        }
    }
}
