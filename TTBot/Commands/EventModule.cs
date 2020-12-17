using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using TTBot.DataAccess;
using TTBot.Extensions;
using TTBot.Models;
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
        private readonly IConfirmationChecks _confirmationChecks;
        private readonly IConfirmationCheckPrinter _confirmationCheckPrinter;
        private readonly IEventParticipantService _eventParticipantService;

        public EventModule(IEvents events, IPermissionService permissionService, IEventSignups eventSignups, IConfirmationChecks confirmationChecks, IConfirmationCheckPrinter confirmationCheckPrinter, IEventParticipantService eventParticipantService)
        {
            _events = events;
            _permissionService = permissionService;
            _eventSignups = eventSignups;
            _confirmationChecks = confirmationChecks;
            _confirmationCheckPrinter = confirmationCheckPrinter;
            _eventParticipantService = eventParticipantService;
        }

        [Command("create")]
        [Alias("add")]
        public async Task Create(string eventName, string shortName, int? capacity = null)
        {
            var author = Context.Message.Author as SocketGuildUser;
            if (!await _permissionService.UserIsModeratorAsync(Context, author))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to create events");
                return;
            }

            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id, Context.Channel.Id);
            var existingEventWithAlias = await _events.GetActiveEvent(shortName, Context.Guild.Id, Context.Channel.Id);
            if (existingEvent != null || existingEventWithAlias != null)
            {
                await Context.Channel.SendMessageAsync("There is already an active event with that name or short name for this channel. Event names must be unique!");
                return;
            }

            string roleId = "";
            try
            {
                /* Create a new role for this event. The role is not visible in the sidebar and can be menitoned.
                 * It is possible that the shortName is already used for a role, but that's not forbidden by Discord.
                 * It might be a consideration for the future to check for duplicates and assign a unique name.
                 */

                var role = await author.Guild.CreateRoleAsync(shortName + " notify", null, null, false, true);
                roleId = role.Id.ToString();
            }
            catch (Discord.Net.HttpException) { /* ignore forbidden exception */ }
            

            var @event = new Models.Event
            {
                ChannelId = Context.Channel.Id.ToString(),
                GuildId = Context.Guild.Id.ToString(),
                RoleId = roleId,
                ShortName = shortName,
                Closed = false,
                Name = eventName,
                Capacity = capacity
            };
            await _events.SaveAsync(@event);
            existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id, Context.Channel.Id);
            await Context.Channel.SendMessageAsync($"{Context.Message.Author.Mention} has created the event {eventName}! React to the message below to sign up to the event. If you can no longer attend, simply remove your reaction!");
            await _eventParticipantService.CreateAndPinParticipantMessage(Context.Channel, existingEvent);
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

            var role = Context.Guild.Roles.FirstOrDefault(x => x.Id.ToString() == existingEvent.RoleId);

            /* the event could have already been deleted by a mod, null-check required */

            if (role != null)
            {
                try
                {
                    await role.DeleteAsync(); 
                }
                catch (Discord.Net.HttpException) { /* ignore forbidden exception */ }
            }

            await _eventParticipantService.UnpinEventMessage(Context.Channel, existingEvent);
            existingEvent.Closed = true;
            await _events.SaveAsync(existingEvent);

            await Context.Channel.SendMessageAsync($"{existingEvent.Name} is now closed!");
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
                await Context.Channel.SendMessageAsync($"Currently active events:{Environment.NewLine}{string.Join(Environment.NewLine, activeEvents.Select(ev => $"{ev.Name}{(ev.SpaceLimited ? $" - {ev.ParticipantCount}/{ev.Capacity} participants" : "")}"))}");
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
            if (await _eventSignups.GetSignupAsync(existingEvent, Context.Message.Author) != null)
            {
                await Context.Message.Author.SendMessageAsync($"You're already signed up to {eventName}");
                return;
            }

            if (existingEvent.SpaceLimited && existingEvent.ParticipantCount >= existingEvent.Capacity)
            {
                await Context.Message.Author.SendMessageAsync($"Sorry, but {eventName} is already full! Keep an eye out in-case someone pulls out.");
                return;
            }

            if (Context.Message.Author is IGuildUser guildUser)
            {
                var role = guildUser.Guild.Roles.FirstOrDefault(x => x.Id.ToString() == existingEvent.RoleId);
                if (role != null)
                {
                    try
                    {
                        await guildUser.AddRoleAsync(role);
                    }
                    catch (Discord.Net.HttpException) { /* ignore forbidden exception */ }
                }
            }

            await _eventSignups.AddUserToEvent(existingEvent, Context.Message.Author as SocketGuildUser);
            await Context.Message.Author.SendMessageAsync($"Thanks {Context.Message.Author.Mention}! You've been signed up to {existingEvent.Name}. You can check the pinned messages in the event's channel to see the list of participants.");
            await UpdateConfirmationCheckForEvent(existingEvent);
            await _eventParticipantService.UpdatePinnedMessageForEvent(Context.Channel, existingEvent);
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
            var existingSignup = await _eventSignups.GetSignupAsync(existingEvent, Context.Message.Author);
            if (existingSignup == null)
            {
                await Context.Channel.SendMessageAsync($"You're not currently signed up to {eventName}");
                return;
            }

            if(Context.Message.Author is IGuildUser guildUser)
            {
                var role = guildUser.Guild.Roles.FirstOrDefault(x => x.Id.ToString() == existingEvent.RoleId);
                if (role != null)
                {
                    /* no effect if user doesn' have the role anymore */
                    try
                    {
                        await guildUser.RemoveRoleAsync(role);
                    }
                    catch (Discord.Net.HttpException) { /* ignore forbidden exception */ }
                }
            }

            await _eventSignups.DeleteAsync(existingSignup);
            await Context.Channel.SendMessageAsync($"Thanks { Context.Message.Author.Mention}! You're no longer signed up to {existingEvent.Name}.");
            await UpdateConfirmationCheckForEvent(existingEvent);
            await _eventParticipantService.UpdatePinnedMessageForEvent(Context.Channel, existingEvent);
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
            var messageText = await _eventParticipantService.GetParticipantsMessageBody(Context.Channel, existingEvent, signUps, showJoinPrompt: false);
            await Context.Channel.SendMessageAsync(messageText);
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
            await UpdateConfirmationCheckForEvent(existingEvent);
            await _eventParticipantService.UpdatePinnedMessageForEvent(Context.Channel, existingEvent);
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
                var existingSignup = await _eventSignups.GetSignupAsync(existingEvent, user);
                if (existingSignup != null)
                {
                    await _eventSignups.DeleteAsync(existingSignup);
                }
            }

            await Context.Channel.SendMessageAsync($"Removed {string.Join(' ', Context.Message.MentionedUsers.Select(user => user.Username))} from {eventName}");
            await UpdateConfirmationCheckForEvent(existingEvent);
            await _eventParticipantService.UpdatePinnedMessageForEvent(Context.Channel, existingEvent);
        }

        [Command("help")]
        public async Task Help()
        {
            await Context.Channel.SendMessageAsync("Use `!events active` to see a list of all active events. To join an event use the `!event join` command with the name of the event. " +
                "For example `!event join ACC Championship`. To unsign from an event, use the `!event unsign` command with the name of the event. For example, `!event unsign ACC Championship`.");
        }

        [Command("confirm")]
        public async Task Confirm([Remainder] string eventName)
        {
            if (!await _permissionService.AuthorIsModerator(Context))
            {
                return;
            }

            var existingEvent = await _events.GetActiveEvent(eventName, Context.Guild.Id, Context.Channel.Id);
            if (existingEvent == null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find an active event with the name {eventName}");
                return;
            }

            var message = await Context.Channel.SendMessageAsync("Starting Confirmation Check..");
            await _confirmationChecks.SaveAsync(new ConfirmationCheck()
            {
                EventId = existingEvent.Id,
                MessageId = message.Id.ToString()
            });
            await _confirmationCheckPrinter.WriteMessage(this.Context.Channel, message, existingEvent);
        }

        private async Task UpdateConfirmationCheckForEvent(EventsWithCount @event)
        {
            var confirmationCheck = await _confirmationChecks.GetMostRecentConfirmationCheckForEventAsync(@event.Id);
            if (confirmationCheck == null)
            {
                return;
            }
            var message = await Context.Channel.GetMessageAsync(Convert.ToUInt64(confirmationCheck.MessageId));
            if (message == null)
            {
                return;
            }
            await _confirmationCheckPrinter.WriteMessage(Context.Channel, (IUserMessage)message, @event);
        }
    }
}