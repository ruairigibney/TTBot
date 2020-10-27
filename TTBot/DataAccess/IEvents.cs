using System.Collections.Generic;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.DataAccess
{
    public interface IEvents
    {
        Task<List<Event>> GetActiveEvents(ulong guildId, ulong channelId);
        Task<Event> GetActiveEvent(string name, ulong guildId, ulong channelId);
        Task SaveAsync(Event @event);
    }
}