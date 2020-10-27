using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace TTBot.Services
{
    public interface IPermissionService
    {
        Task<bool> UserIsModerator(SocketCommandContext context, SocketGuildUser user);
    }
}