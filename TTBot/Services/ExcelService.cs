using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public async Task<List<ExcelChampionshipRoundModel>> DeriveRoundsFromAttachment(Attachment attachment)
        {
            return await _excelPackage.GetChampionshipRoundsData(attachment);
        }

        public async Task<List<ExcelDriverDataModel>> ReadResultsDataFromAttachment(Attachment attachment)
        {
            return await _excelPackage.GetExcelDriverData(attachment);
        }
    }
}
