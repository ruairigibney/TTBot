using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTBot.DataAccess;
using TTBot.Extensions;
using TTBot.Models;

namespace TTBot.Services
{
    public class EventParticipantService : IEventParticipantService
    {
        private readonly IEventSignups _eventSignups;
        private readonly IEvents _events;

        public EventParticipantService(IEventSignups eventSignups, IEvents events)
        {
            _eventSignups = eventSignups;
            _events = events;
        }

        public async Task<IMessage> CreateAndPinParticipantMessage(ISocketMessageChannel channel, EventsWithCount @event)
        {
            var signups = await _eventSignups.GetAllSignupsForEvent(@event);
            var message = await channel.SendMessageAsync(await GetParticipantsMessageBody(channel, @event, signups));
            await message.PinAsync();
            @event.MessageId = message.Id.ToString();
            await _events.SaveAsync(@event);
            return message;
        }

        public async Task<string> GetParticipantsMessageBody(ISocketMessageChannel channel, EventsWithCount @event, List<EventSignup> signups)
        {
            var users = await Task.WhenAll(signups.Select(async sup => (await channel.GetUserAsync(Convert.ToUInt64(sup.UserId)) as SocketGuildUser)));

            var usersInEvent = string.Join(Environment.NewLine, users.Select(u => u.GetDisplayName()));
            var message = $"**{@event.Name}**{Environment.NewLine}";
            if (@event.SpaceLimited)
            {
                message += $"There's {users.Length} out of {@event.Capacity}";
            }
            else
            {
                message += $"There's {users.Length}";
            }

            message += $" racers signed up for {@event.Name}.{Environment.NewLine}{ usersInEvent}{Environment.NewLine}{Environment.NewLine}";

            if (!@event.Full)
            {
                message += $"Sign up to this event with `!event join {@event.DisplayName}`";
            }
            else if (@event.SpaceLimited && @event.Full)
            {
                message += "This event is currently at capacity. Keep an eye out in case somebody unsigns.";
            }

            return message;
        }

        public async Task UpdatePinnedMessageForEvent(ISocketMessageChannel channel, EventsWithCount @event)
        {
            IMessage message;
            if (@event.MessageId == null || (message = await channel.GetMessageAsync(Convert.ToUInt64(@event.MessageId))) == null)
            {
                await CreateAndPinParticipantMessage(channel, @event);
                return;
            }
            var messageBody = await GetParticipantsMessageBody(channel, @event, await _eventSignups.GetAllSignupsForEvent(@event));
            await ((IUserMessage)message).ModifyAsync(prop => prop.Content = messageBody);
        }

        public async Task UnpinEventMessage(ISocketMessageChannel channel, Event @event)
        {
            if (@event.MessageId == null)
            {
                return;
            }
            IUserMessage msg = ((IUserMessage)await channel.GetMessageAsync(Convert.ToUInt64(@event.MessageId)));
            if (msg != null)
            {
                await msg.UnpinAsync();
            }
        }

    }
}
