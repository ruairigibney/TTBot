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
                        ChampionshipId = resultsModel.ChampionshipId,
                        Driver = resultsModel.Driver,
                        Points = resultsModel.Points,
                        Positions = resultsModel.Positions
                    });
                }
            }
        }

        public async Task<List<ChampionshipResultsModel>> GetChampionshipResultsById(int championshipId)
        {
            using (var connection = _conFactory.Open())
            {
                return await connection.SelectAsync<ChampionshipResultsModel>(r => r.ChampionshipId == championshipId);
            }
        }
    }
}
