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
    public class Championships : IChampionships
    {
        private readonly IDbConnectionFactory _conFactory;

        public Championships(IDbConnectionFactory conFactory)
        {
            _conFactory = conFactory;
        }

        public async Task<int> Exists(string championship)
        {
            using (var connection = _conFactory.Open())
            {
                return connection.QueryAsync<int>(
                    $"SELECT Id FROM Championship WHERE Name = '{championship}'").Result.FirstOrDefault();
            }
        }

        public async Task<int> AddAsync(string championship)
        {
            using (var connection = _conFactory.Open())
            {
                var newChampionship = await connection.InsertAsync(new Championship()
                {
                    Name = championship
                });

                return (int)newChampionship;
            }
        }

        public async Task<IEnumerable<Championship>> GetAllAsync()
        {
            using (var connection = _conFactory.Open())
            {
                return await connection.SelectAsync<Championship>();
            }
        }

        public Task UpdateAsync(Championship championship)
        {
            throw new NotImplementedException();
        }
    }
}
