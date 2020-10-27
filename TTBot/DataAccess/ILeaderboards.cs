using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.DataAccess
{
    public interface ILeaderboards
    {
        Task AddAsync(ulong guildId, ulong channelId, string game, DateTime? endDate = null, bool active = true);
        Task<Leaderboard> GetActiveLeaderboardForChannelAsync(ulong guildId, ulong channel);
        Task<IEnumerable<Leaderboard>> GetAllAsync(ulong guildId);
        Task UpdateAsync(Leaderboard leaderboard);
        Task<IEnumerable<Leaderboard>> GetAllActiveAsync(ulong guildId);
        Task<IEnumerable<LeaderboardEntry>> GetStandingsAsync(int leaderboardId);
    }
}