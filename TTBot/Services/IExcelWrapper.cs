using Discord;
using System.Collections.Generic;
using TTBot.Models;

namespace TTBot.Services
{
    public interface IExcelWrapper
    {
        public List<ExcelDriverDataModel> GetExcelDriverData(Attachment attachment);
    }
}