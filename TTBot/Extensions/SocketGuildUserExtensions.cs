using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
namespace TTBot.Extensions
{
    public static class SocketGuildUserExtensions
    {
        public static string GetDisplayName(this SocketGuildUser @this)
        {
            return string.IsNullOrEmpty(@this.Nickname) ? @this.Username : @this.Nickname;
        }
    }
}
