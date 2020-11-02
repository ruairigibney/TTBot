using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTBot.DataAccess;
using TTBot.Extensions;
using TTBot.Models;

namespace TTBot.Services
{
    public class ConfirmationCheckPrinter : IConfirmationCheckPrinter
    {
        private readonly IEventSignups _eventSignups;

        public ConfirmationCheckPrinter(IEventSignups eventSignups)
        {
            _eventSignups = eventSignups;
        }

        public async Task WriteMessage(ISocketMessageChannel channel, IUserMessage message, EventsWithCount @event)
        {
            var messageContents = $"**Confirmation Check for {@event.Name}!{Environment.NewLine}" +
                $"Please confirm your attendance by reacting to this message.**{Environment.NewLine}{Environment.NewLine}";
            if (!@event.Closed && !@event.Full)
            {
                messageContents += $"You can still sign up to this event by typing `!event join {@event.DisplayName}`{Environment.NewLine}{Environment.NewLine}";
            }

            var signUps = await _eventSignups.GetAllSignupsForEvent(@event);
            var confirmed = new List<IUser>();

            foreach (var reaction in message.Reactions)
            {
                var reactors = await message.GetReactionUsersAsync(reaction.Key, 999).FlattenAsync();
                confirmed.AddRange(reactors);
            }

            confirmed = confirmed.Distinct(new UserEqualityComparer()).Where(confirmed => signUps.Any(sup => sup.UserId == confirmed.Id.ToString())).ToList();
            var unconfirmed = signUps.Where(signUp => !confirmed.Any(con => con.Id.ToString() == signUp.UserId));
            var channelUsers = await channel.GetUsersAsync().FlattenAsync();
            var unconfirmedUsers = unconfirmed.Select(u => channelUsers.FirstOrDefault(cu => cu.Id.ToString() == u.UserId)).ToList();

            messageContents += $"**Confirmed:**{Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, confirmed.Select(con => con?.Mention ?? "Unknown User"))}{Environment.NewLine}";
            messageContents += $"**Unconfirmed:**{Environment.NewLine}" +
                 $"{string.Join(Environment.NewLine, unconfirmedUsers.Select(con => con?.Mention ?? "Unknown User"))}{Environment.NewLine}";


            await message.ModifyAsync((msg) => msg.Content = messageContents);
        }
    }

    class UserEqualityComparer : IEqualityComparer<IUser>
    {
        public bool Equals([AllowNull] IUser x, [AllowNull] IUser y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode([DisallowNull] IUser obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
