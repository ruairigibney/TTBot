using ServiceStack.Data;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TTBot.Models;
using System.Linq;

namespace TTBot.DataAccess
{
    public class Events : IEvents
    {
        private readonly IDbConnectionFactory _conFactory;

        public Events(IDbConnectionFactory conFactory)
        {
            _conFactory = conFactory;
        }

        public async Task SaveAsync(Event @event)
        {
            using (var con = _conFactory.Open())
            {
                await con.SaveAsync(@event);
            }
        }

        public async Task<Event> GetActiveEvent(string name, ulong guildId, ulong channelId)
        {
            using (var con = _conFactory.Open())
            {
                return (await con.SelectAsync<Event>(ev => ev.Name == name && ev.GuildId == guildId.ToString() && ev.ChannelId == channelId.ToString() && !ev.Closed)).SingleOrDefault();
            }
        }

        public async Task<List<Event>> GetActiveEvents(ulong guildId, ulong channelId)
        {
            using (var con = _conFactory.Open())
            {
                return await con.SelectAsync<Event>(ev => ev.GuildId == guildId.ToString() && ev.ChannelId == channelId.ToString() && !ev.Closed);
            }
        }
    }
}
