using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.DataAccess
{
    public interface IEventSignups
    {
        Task AddUserToEvent(Event @event, SocketUser user);
        Task<List<EventSignup>> GetAllSignupsForEvent(Event @event);
        Task<EventSignup> GetSignUp(Event @event, SocketUser user);
        Task Delete(EventSignup signup);
        Task SaveAsync(EventSignup eventSignUp);
    }
}