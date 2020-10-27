using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using TTBot.DataAccess;
using TTBot.Extensions;
using TTBot.Services;

namespace TTBot.Commands
{
    [Group("event")]
    [Alias("events")]
    public class EventModule : ModuleBase<SocketCommandContext>
    {
        private readonly IEvents _events;
        private readonly IPermissionService _permissionService;
        private readonly IEventSignups _eventSignups;

        public EventModule(IEvents events, IPermissionService permissionService, IEventSignups eventSignups)
        {
            _events = events;
            _permissionService = permissionService;
            _eventSignups = eventSignups;
        }

        [Command("create")]
        [Alias("add")]
        public async Task Create([Remainder] string eventName)
        {
            var author = Context.Message.Author as SocketGuildUser;
            if (!await _permissionService.UserIsModeratorAsync(Context, author))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to create events");
                return;
            }

            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id, Context.Channel.Id);

            if (existingEvent != null)
            {
                await Context.Channel.SendMessageAsync("There is already an active event with that name for this channel. Event names must be unique!");
                return;
            }

            await _events.SaveAsync(new Models.Event
            {
                ChannelId = Context.Channel.Id.ToString(),
                GuildId = Context.Guild.Id.ToString(),
                Closed = false,
                Name = eventName
            });

            await Context.Channel.SendMessageAsync($"{Context.Message.Author.Mention} has created the event {eventName}! Sign up to the event by typing `!event signup {eventName}` in this channel. If you've signed up and can no longer attend, use the command `!event unsign {eventName}`");
        }

        [Command("close")]
        [Alias("delete")]
        public async Task Close([Remainder] string eventName)
        {
            var author = Context.Message.Author as SocketGuildUser;
            if (!await _permissionService.UserIsModeratorAsync(Context, author))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to create events");
                return;
            }

            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id, Context.Channel.Id);
            if (existingEvent == null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find an active event with the name {eventName}");
                return;
            }

            existingEvent.Closed = true;
            await _events.SaveAsync(existingEvent);

            await Context.Channel.SendMessageAsync($"{eventName} is now closed!");
        }

        [Command("active")]
        [Alias("current", "open")]
        public async Task ActiveEvents()
        {
            var activeEvents = await _events.GetActiveEvents(Context.Guild.Id, Context.Channel.Id);
            if (!activeEvents.Any())
            {
                await Context.Channel.SendMessageAsync($"There's no events currently running for {Discord.MentionUtils.MentionChannel(Context.Channel.Id)}.");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Currently active events:{Environment.NewLine}{string.Join(Environment.NewLine, activeEvents.Select(ev => ev.Name))}");
                await Context.Channel.SendMessageAsync($"Join any active event with the command `!event signup event name`");
            }
        }

        [Command("signup")]
        [Alias("sign", "join")]
        public async Task SignUp([Remainder] string eventName)
        {
            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id, Context.Channel.Id);
            if (existingEvent == null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find an active event with the name {eventName}");
                return;
            }
            var existingSignup = await _eventSignups.GetSignUp(existingEvent, Context.Message.Author);
            if (existingSignup != null)
            {
                await Context.Channel.SendMessageAsync($"You're already signed up to {eventName}");
                return;
            }
            await _eventSignups.AddUserToEvent(existingEvent, Context.Message.Author as SocketGuildUser);
            await Context.Channel.SendMessageAsync($"Thanks {Context.Message.Author.Mention}! You've been signed up to {eventName}. ");
            await GetSignups(eventName);
        }

        [Command("unsign")]
        [Alias("unsignup")]
        public async Task Unsign([Remainder] string eventName)
        {
            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id, Context.Channel.Id);
            if (existingEvent == null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find an active event with the name {eventName}");
                return;
            }
            var existingSignup = await _eventSignups.GetSignUp(existingEvent, Context.Message.Author);
            if (existingSignup == null)
            {
                await Context.Channel.SendMessageAsync($"You're not currently signed up to {eventName}");
                return;
            }
            await _eventSignups.Delete(existingSignup);
            await Context.Channel.SendMessageAsync($"Thanks { Context.Message.Author.Mention}! You're no longer signed up to {eventName}.");
            await GetSignups(eventName);
        }

        [Command("signups")]
        [Alias("participants")]
        public async Task GetSignups([Remainder] string eventName)
        {
            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id, Context.Channel.Id);
            if (existingEvent == null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find an active event with the name {eventName}");
                return;
            }

            var signUps = await _eventSignups.GetAllSignupsForEvent(existingEvent);
            var users = await Task.WhenAll(signUps.Select(async sup => (await Context.Channel.GetUserAsync(Convert.ToUInt64(sup.UserId)) as SocketGuildUser)));
            await Context.Channel.SendMessageAsync($"There's {users.Length} racers signed up for {eventName}.{Environment.NewLine}{string.Join(Environment.NewLine, users.Select(u => u.GetDisplayName()))}");
        }

        [Command("bulkadd", ignoreExtraArgs: true)]
        [Alias("bulksign")]
        public async Task BulkAdd(string eventName)
        {
            var author = Context.Message.Author as SocketGuildUser;
            if (!await _permissionService.UserIsModeratorAsync(Context, author))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to bulk add");
                return;
            }

            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id, Context.Channel.Id);
            if (existingEvent == null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find an active event with the name {eventName}");
                return;
            }

            await Task.WhenAll(Context.Message.MentionedUsers.Select(async user => await _eventSignups.AddUserToEvent(existingEvent, user)));
            await GetSignups(eventName);
        }

        [Command("remove", ignoreExtraArgs: true)]
        public async Task Remove(string eventName)
        {
            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id, Context.Channel.Id);
            if (existingEvent == null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find an active event with the name {eventName}");
                return;
            }

            foreach (var user in Context.Message.MentionedUsers)
            {
                var existingSignup = await _eventSignups.GetSignUp(existingEvent, user);
                if (existingSignup != null)
                {
                    await _eventSignups.Delete(existingSignup);
                }
            }

            await Context.Channel.SendMessageAsync($"Removed {string.Join(' ', Context.Message.MentionedUsers.Select(user => user.Username))} from {eventName}");
            await GetSignups(eventName);
        }

        [Command("help")]
        public async Task Help()
        {
            await Context.Channel.SendMessageAsync("Use `!events active` to see a list of all active events. To join an event use the `!event signup` command with the name of the event. " +
                "For example `!event signup ACC Championship`. To unsign from an event, use the `!event unsign` command with the name of the event. For example, `!event unsign ACC Championship`.");
        }
    }
}