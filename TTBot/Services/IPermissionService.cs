﻿using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace TTBot.Services
{
    public interface IPermissionService
    {
        Task<bool> AuthorIsModerator(SocketCommandContext context, string errorMessage = null);
        Task<bool> UserIsModeratorAsync(SocketCommandContext context, SocketGuildUser user);
    }
}