using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.DataAccess
{
    public interface IConfirmationChecks
    {
        Task<ConfirmationCheck> GetConfirmationCheckByMessageId(ulong messageId);
        Task SaveAsync(ConfirmationCheck confirmationCheck);
        Task<ConfirmationCheck> GetMostRecentConfirmationCheckForEventAsync(int eventId);
    }
}