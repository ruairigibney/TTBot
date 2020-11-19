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
        Task<string> GetParticipantsMessageBody(ISocketMessageChannel channel, EventsWithCount @event, List<EventSignup> signups);
        Task UnpinEventMessage(ISocketMessageChannel channel, Event @event);
        Task UpdatePinnedMessageForEvent(ISocketMessageChannel channel, EventsWithCount @event);
    }
}