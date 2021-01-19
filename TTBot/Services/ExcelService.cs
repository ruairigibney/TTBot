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
        private readonly IChampionships _championships;

        public ExcelService(IExcelWrapper excelPackage, IChampionships championships)
        {
            _excelPackage = excelPackage ?? throw new ArgumentNullException(nameof(excelPackage));
            _championships = championships ?? throw new ArgumentNullException(nameof(championships));
        }

        public async Task<List<ExcelDataModel>> ReadResultsDataFromAttachment(Attachment attachment)
        {
            return _excelPackage.GetExcelData(attachment);
        }
    }
}
