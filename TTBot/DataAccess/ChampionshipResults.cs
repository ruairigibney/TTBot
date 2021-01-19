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
    public class ChampionshipResults : IChampionshipResults
    {
        private readonly IDbConnectionFactory _conFactory;

        public ChampionshipResults(IDbConnectionFactory conFactory)
        {
            _conFactory = conFactory;
        }

        public async Task AddAsync(List<ChampionshipResultsModel> championshipResults)
        {
            using (var connection = _conFactory.Open())
            {
                foreach (ChampionshipResultsModel resultsModel in championshipResults)
                {
                    await connection.InsertAsync(new ChampionshipResultsModel()
                    {
                        EventId = resultsModel.EventId,
                        Driver = resultsModel.Driver,
                        Points = resultsModel.Points,
                        Positions = resultsModel.Positions
                    });
                }
            }
        }

        public async Task DeleteAllAsync<ChampionshipResulstModel>()
        {
            using (var connection = _conFactory.Open())
            {
                await connection.DeleteAllAsync<ChampionshipResultsModel>();
            }
        }

        public async Task<List<ChampionshipResultsModel>> GetChampionshipResultsByIdAsync(int eventId)
        {
            using (var connection = _conFactory.Open())
            {
                return await connection.SelectAsync<ChampionshipResultsModel>(r => r.EventId == eventId);
            }
        }

        public async Task<string[]> GetEventsWithResultsAsync()
        {
            using (var connection = _conFactory.Open())
            {
                var q = connection.From<ChampionshipResultsModel>()
                    .Join<ChampionshipResultsModel, Event>();

                var results = await connection.SelectAsync<Event>(q);

                return results.Select(r => r.ShortName).Distinct().ToArray<string>();
            }
        }

    }
}
