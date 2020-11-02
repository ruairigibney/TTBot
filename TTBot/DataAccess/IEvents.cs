using System.Collections.Generic;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.DataAccess
{
    public interface IEvents
    {
        Task<List<EventsWithCount>> GetActiveEvents(ulong guildId, ulong channelId);
        Task<EventsWithCount> GetActiveEvent(string name, ulong guildId, ulong channelId);
        Task SaveAsync(Event @event);
        Task<EventsWithCount> GetActiveEvent(int eventId);
    }
}