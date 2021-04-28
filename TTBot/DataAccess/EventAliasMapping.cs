using ServiceStack.Data;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.DataAccess
{
    public class EventAliasMapping : IEventAliasMapping
    {
        private readonly IDbConnectionFactory _conFactory;

        public EventAliasMapping(IDbConnectionFactory conFactory)
        {
            _conFactory = conFactory;
        }

        public async Task AddAsync(ulong eventId, string alias)
        {
            using (var connection = _conFactory.Open())
            {
                await connection.InsertAsync(new EventAliasMappingModel()
                {
                    EventId = eventId,
                    Alias = alias
                });
            }
        }

        public async Task RemoveAsync(int id)
        {
            using (var connection = _conFactory.Open())
            {
                var q = connection.From<EventAliasMappingModel>().Where(em => em.Id == id);

                await connection.DeleteAsync(q);
            }
        }

        public async Task<Event> GetActiveEventFromAliasAsync(string alias, ulong guildId)
        {
            using (var connection = _conFactory.Open())
            {
                {
                    var q = connection.From<EventAliasMappingModel>()
                        .Join<EventAliasMappingModel, Event>()
                        .Where<Event>(e => e.Closed == false && e.GuildId == guildId.ToString());

                    var mappings = await connection.SelectMultiAsync<EventAliasMappingModel, Event>(q);
                    return mappings.Find(em => getLowerTrimmedText(em.Item1.Alias) == getLowerTrimmedText(alias))?.Item2;
                }
            }
        }

        public async Task<int> GetAliasIdAsync(string alias, ulong guildId)
        {
            using (var connection = _conFactory.Open())
            {
                var q = connection.From<EventAliasMappingModel>()
                    .Join<EventAliasMappingModel, Event>()
                    .Where<Event>(e => e.Closed == false && e.GuildId == guildId.ToString());

                var mappings = await connection.SelectAsync(q);
                return mappings.Find(em => getLowerTrimmedText(em.Alias) == getLowerTrimmedText(alias)).Id;
            }
        }

        private string getLowerTrimmedText(string s)
        {
            return s?.Replace(" ", "").ToLower();
        }


        public async Task<bool> ActiveEventExistsAsync(string alias, ulong guildId)
        {
            using (var connection = _conFactory.Open())
            {
                var q = connection.From<EventAliasMappingModel>()
                    .Join<EventAliasMappingModel, Event>()
                    .Where<Event>(e => e.Closed == false && e.GuildId == guildId.ToString());

                var mappings = await connection.SelectAsync(q);
                return mappings.Find(em => getLowerTrimmedText(em.Alias) == getLowerTrimmedText(alias)) != null;
            }
        }

        public async Task<List<EventAliasMappingModel>> GetAllActiveAliases()
        {
            using (var connection = _conFactory.Open())
            {
                var q = connection.From<EventAliasMappingModel>()
                    .OrderBy(em => em.EventId);

                return await connection.SelectAsync(q);
            }
        }
    }
}
