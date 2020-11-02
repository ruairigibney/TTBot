using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.Services
{
    public interface IConfirmationCheckPrinter
    {
        Task WriteMessage(ISocketMessageChannel channel, IUserMessage message, EventsWithCount @event);
    }
}