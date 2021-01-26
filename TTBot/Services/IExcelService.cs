using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.Services
{
    public interface IExcelService
    {
        Task<List<ExcelDriverDataModel>> ReadResultsDataFromAttachment(Attachment attachment);
    }
}
