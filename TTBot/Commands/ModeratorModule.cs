using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTBot.DataAccess;

namespace TTBot.Commands
{
    [Group("mod")]
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        private readonly IModerator _moderator;

        public ModeratorModule(IModerator moderator)
        {
            _moderator = moderator;
        }

        [Command("add", ignoreExtraArgs: true)]
        [Summary("Gives a Role permission to add/remove and moderate leaderboards for this server")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Add()
        {
            foreach (var role in Context.Message.MentionedRoles)
            {
                await _moderator.AddRoleAsModerator(this.Context.Guild.Id, role.Id);
                await Context.Channel.SendMessageAsync(role.Name + " can now manage leaderboards");
            }
            return;
        }

        [Command("remove", ignoreExtraArgs: true)]
        [Summary("Removes a users permission to add/remove and moderate leaderboards for this server")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Remove()
        {
            foreach (var role in Context.Message.MentionedRoles)
            {
                await _moderator.RemoveRoleAsModeratorAsync(this.Context.Guild.Id, role.Id);
                await Context.Channel.SendMessageAsync(role.Name + " can no longer manage leaderboards");
            }
            return;
        }

        [Command("list", ignoreExtraArgs: true)]
        [Summary("Lists users with permission to add/remove and moderate leaderboards for this server")]
        public async Task List()
        {

            var guild = ((SocketGuildChannel)this.Context.Channel).Guild;
            var moderators = await _moderator.GetLeaderboardModeratorsAsync(guild.Id);
            await this.Context.Channel.SendMessageAsync($"Leaderboard moderator roles: {string.Join(", ", moderators.Select(mod => guild.GetRole(Convert.ToUInt64(mod.RoleId))).Select(role => role.Name))}");
        }
    }

}
