using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTBot.DataAccess;

namespace TTBot.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IModerator _moderator;

        public PermissionService(IModerator moderator)
        {
            _moderator = moderator;
        }

        public async Task<bool> UserIsModeratorAsync(SocketCommandContext context, SocketGuildUser user)
        {
            if (user.GuildPermissions.ManageGuild || user.Id == 122447770140409856)
            {
                return true;
            }

            var moderatorRoles = await _moderator.GetLeaderboardModeratorsAsync(context.Guild.Id);
            return user.Roles.Any(role => moderatorRoles.Any(mR => role.Id == Convert.ToUInt64(mR.RoleId)));
        }

        public async Task<bool> AuthorIsModerator(SocketCommandContext context, string errorMessage = null)
        {
            var author = context.Message.Author as SocketGuildUser;
            if (!await UserIsModeratorAsync(context, author))
            {
                await context.Channel.SendMessageAsync(errorMessage ?? "You dont have permission to run this command");
                return false;
            }

            return true;
        }

    }
}
