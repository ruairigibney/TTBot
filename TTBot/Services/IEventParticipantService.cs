using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.Services
{
    public interface IEventParticipantService
    {
        Task<IMessage> CreateAndPinParticipantMessage(ISocketMessageChannel channel, EventsWithCount @event);
        Task<Embed> GetParticipantsEmbed(ISocketMessageChannel channel, EventsWithCount @event, List<EventSignup> signups, bool showJoinPrompt = true);
        Task<string> GetParticipantsMessageBody(ISocketMessageChannel channel, EventsWithCount @event, List<EventSignup> signups, bool showJoinPrompt = true);
        Task UnpinEventMessage(ISocketMessageChannel channel, Event @event);
        Task UpdatePinnedMessageForEvent(ISocketMessageChannel channel, EventsWithCount @event);
        Task UpdatePinnedMessageForEvent(ISocketMessageChannel channel, EventsWithCount @event, IUserMessage message);
    }
}