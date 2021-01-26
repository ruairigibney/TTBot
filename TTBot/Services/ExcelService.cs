using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TTBot.DataAccess;
using TTBot.Models;

namespace TTBot.Services
{
    public class ExcelService : IExcelService
    {
        private readonly IExcelWrapper _excelPackage;

        public ExcelService(IExcelWrapper excelPackage)
        {
            _excelPackage = excelPackage ?? throw new ArgumentNullException(nameof(excelPackage));
        }

        public async Task<List<ExcelDriverDataModel>> ReadResultsDataFromAttachment(Attachment attachment)
        {
            return _excelPackage.GetExcelDriverData(attachment);
        }
    }
}
