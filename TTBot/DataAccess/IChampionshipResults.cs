using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.DataAccess
{
    public interface IChampionshipResults
    {
        Task AddAsync(List<ChampionshipResultsModel> championshipResults);
        Task<List<ChampionshipResultsModel>> GetChampionshipResultsByIdAsync(int championshipId);
        Task DeleteAllGuildEvents<ChampionshipResultsModel>(string guildId);
        Task<string[]> GetEventsWithResultsAsync();
    }
}
