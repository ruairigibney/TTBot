using System.Collections.Generic;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.DataAccess
{
    public interface IEventAliasMapping
    {
        Task AddAsync(ulong eventId, string alias);
        Task RemoveAsync(int id);
        Task<Event> GetActiveEventFromAliasAsync(string alias);
        Task<int> GetAliasIdAsync(string alias);
        Task<bool> ActiveEventExistsAsync(string alias);
        Task<List<EventAliasMappingModel>> GetAllActiveAliases();
    }
}
