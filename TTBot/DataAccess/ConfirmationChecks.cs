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
    public class ConfirmationChecks : IConfirmationChecks
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public ConfirmationChecks(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task SaveAsync(ConfirmationCheck confirmationCheck)
        {
            using (var con = await _dbConnectionFactory.OpenAsync())
            {
                await con.SaveAsync(confirmationCheck);
            }
        }

        public async Task<ConfirmationCheck> GetConfirmationCheckByMessageId(ulong messageId)
        {
            using (var con = await _dbConnectionFactory.OpenAsync())
            {
                return await con.SingleAsync<ConfirmationCheck>(conCheck => conCheck.MessageId == messageId.ToString());
            }
        }

        public async Task<ConfirmationCheck> GetMostRecentConfirmationCheckForEventAsync(int eventId)
        {
            using (var con = await _dbConnectionFactory.OpenAsync())
            {
                return (await con.SelectAsync(con.From<ConfirmationCheck>().Where(conCheck => conCheck.EventId == eventId).OrderByDescending(conCheck => conCheck.CreatedDate).Take(1))).FirstOrDefault();
            }
        }
    }
}
