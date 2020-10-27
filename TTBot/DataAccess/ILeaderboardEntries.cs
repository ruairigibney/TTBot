using System;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.DataAccess
{
    public interface ILeaderboardEntries
    {
        Task AddAsync(int leaderboardId, TimeSpan time, ulong userId, string proofUrl);
        Task<LeaderboardEntry> GetBestEntryForUser(int leaderboardId, ulong userId);
        Task Update(LeaderboardEntry entry);
    }
}