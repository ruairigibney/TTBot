using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.DataAccess
{
    public interface IChampionships
    {
        Task<int> AddAsync(string championship);
        Task<IEnumerable<Championship>> GetAllAsync();
        Task UpdateAsync(Championship championship);
        Task<int> Exists(string championship);
    }
}