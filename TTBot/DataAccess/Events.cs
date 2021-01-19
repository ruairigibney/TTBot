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

        public async Task<EventsWithCount> GetActiveEvent(string name, ulong guildId, ulong channelId)
        {
            using (var con = await _conFactory.OpenAsync())
            {
                return (await con.SelectAsync<EventsWithCount>(ev => (ev.Name.ToLower() == name.ToLower() || (ev.ShortName != null && ev.ShortName.ToLower() == name.ToLower())) && ev.GuildId == guildId.ToString() && ev.ChannelId == channelId.ToString() && !ev.Closed)).SingleOrDefault();
            }
        }

        public async Task<List<EventsWithCount>> GetActiveEvents(ulong guildId, ulong channelId)
        {
            using (var con = await _conFactory.OpenAsync())
            {
                return await con.SelectAsync<EventsWithCount>(ev => ev.GuildId == guildId.ToString() && ev.ChannelId == channelId.ToString() && !ev.Closed);
            }
        }

        public async Task<EventsWithCount> GetActiveEvent(int eventId)
        {
            using (var con = _conFactory.Open())
            {
                return await con.SingleAsync<EventsWithCount>(e => e.Id == eventId && !e.Closed);
            }
        }
        public async Task<EventsWithCount> GetEventByMessageIdAsync(ulong messageId)
        {
            using (var con = _conFactory.Open())
            {
                return await con.SingleAsync<EventsWithCount>(e => e.MessageId == messageId.ToString());
            }
        }

        public async Task<EventsWithCount> GetEventByShortname(string shortname)
        {
            using (var con = await _conFactory.OpenAsync())
            {
                return (await con.SelectAsync<EventsWithCount>(ev => (ev.ShortName.ToLower() == shortname.ToLower()))).SingleOrDefault();
            }
        }
    }
}
