using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.DataAccess
{
    public interface IEventSignups
    {
        Task AddUserToEvent(Event @event, IUser user);
        Task<List<EventSignup>> GetAllSignupsForEvent(Event @event);
        Task<EventSignup> GetSignupAsync(Event @event, IUser user);
        Task DeleteAsync(EventSignup signup);
        Task SaveAsync(EventSignup eventSignUp);
    }
}