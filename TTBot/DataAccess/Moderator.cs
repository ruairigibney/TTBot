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
    public class Moderator : IModerator
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public Moderator(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task AddRoleAsModerator(ulong guildId, ulong roleId)
        {
            using (var connection = _dbConnectionFactory.Open())
            {
                await connection.InsertAsync(new LeaderboardModerator()
                {
                    GuildId = guildId.ToString(),
                    RoleId = roleId.ToString()
                });
            }
        }


        public async Task RemoveRoleAsModeratorAsync(ulong guildId, ulong roleId)
        {
            using (var connection = _dbConnectionFactory.Open())
            {
                await connection.ExecuteAsync("DELETE FROM LeaderboardModerators where guildId = @guildId and roleId = @roleId ", new { guildId = guildId.ToString(), roleId = roleId.ToString() });
            }
        }

        public async Task<LeaderboardModerator> GetLeaderboardModeratorAsync(ulong guildId, ulong roleId)
        {
            using (var connection = _dbConnectionFactory.Open())
            {
                try
                {
                    return await connection.QueryFirstOrDefaultAsync<LeaderboardModerator>("" +
                  "SELECT * " +
                  "FROM LeaderboardModerators " +
                  "where guildId = @guildId and roleId =@roleId", new { guildId = guildId.ToString(), roleId = roleId.ToString() });
                }
                catch (Exception ex)
                {
                    throw;
                }

            }
        }

        public async Task<List<LeaderboardModerator>> GetLeaderboardModeratorsAsync(ulong guildId)
        {
            using (var connection = _dbConnectionFactory.Open())
            {
                return (await connection.QueryAsync<LeaderboardModerator>("SELECT * FROM LeaderboardModerators where guildId = @guildId", new { guildId = (long)guildId })).ToList();
            }
        }
    }
}
