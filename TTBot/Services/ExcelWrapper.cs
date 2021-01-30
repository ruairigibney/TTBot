using Discord;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TTBot.DataAccess;
using TTBot.Models;

namespace TTBot.Services
{
    public class ExcelWrapper : IExcelWrapper
    {
        private readonly IExcelSheetEventMapping _excelSheetEventMapping;
        public ExcelWrapper(IExcelSheetEventMapping excelSheetEventMapping)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            _excelSheetEventMapping = excelSheetEventMapping ?? throw new ArgumentNullException(nameof(IExcelSheetEventMapping));
        }

        public async Task<List<ExcelChampionshipRoundModel>> GetChampionshipRoundsData(Attachment attachment)
        {
            List<ExcelChampionshipRoundModel> excelChampionshipRounds = new List<ExcelChampionshipRoundModel>();

            WebClient webClient = new WebClient();
            byte[] buffer = webClient.DownloadData(attachment.Url);
            using MemoryStream stream = new MemoryStream(buffer);
            using (var package = new ExcelPackage(stream))
            {
                foreach (ExcelWorksheet worksheet in package.Workbook.Worksheets)
                {
                    var eventShortname = await _excelSheetEventMapping.GetEventShortnameFromSheetNameAsync(worksheet.Name, true);

                    if (worksheet.Dimension == null || eventShortname == null)
                    {
                        continue;
                    }

                    int rowCount = worksheet.Dimension.End.Row;
                    int colCount = worksheet.Dimension.End.Column;
                    int maxRound = 0;

                    for (int c = 0; c < colCount; c++)
                    {
                        var col = c + 1;

                        if (worksheet.Cells[1, col].Text.ToLower().Contains("round")) {
                            var roundBeingRead = worksheet.Cells[1, col].Text.ToLower().Replace("round", "").Trim();
                            for (int r = 0; r < colCount; r++)
                            {
                                var row = r + 5;

                                if (!string.IsNullOrWhiteSpace(worksheet.Cells[row, col].Text)) {
                                    maxRound = int.Parse(roundBeingRead);
                                    c++; // skip the Fast Lap column
                                    break;
                                }
                            }
                        }
                    }

                    excelChampionshipRounds.Add(new ExcelChampionshipRoundModel()
                    {
                        Championship = eventShortname,
                        Round = maxRound
                    });
                }
            }

            return excelChampionshipRounds;
        }

        public async Task<List<ExcelDriverDataModel>> GetExcelDriverData(Attachment attachment)
        {
            List<ExcelDriverDataModel> excelDriverDataModels = new List<ExcelDriverDataModel>();

            WebClient webClient = new WebClient();
            byte[] buffer = webClient.DownloadData(attachment.Url);
            using MemoryStream stream = new MemoryStream(buffer);
            using (var package = new ExcelPackage(stream))
            {
                foreach (ExcelWorksheet worksheet in package.Workbook.Worksheets)
                {
                    var eventShortname = await _excelSheetEventMapping.GetEventShortnameFromSheetNameAsync(worksheet.Name, false);

                    if (worksheet.Dimension == null || eventShortname == null)
                    {
                        continue;
                    }

                    int rowCount = worksheet.Dimension.End.Row;
                    int colCount = worksheet.Dimension.End.Column;

                    for (int i = 0; i < rowCount; i++)
                    {
                        var row = i + 3;
                        if (string.IsNullOrWhiteSpace(worksheet.Cells[row, 3].Text))
                        {
                            continue;
                        }
                        ExcelDriverDataModel excelDriverDataModel = new ExcelDriverDataModel()
                        {
                            Championship = eventShortname,
                            Pos = Int32.Parse(worksheet.Cells[row, 2].Text),
                            Driver = worksheet.Cells[row, 3].Text,
                            Number = worksheet.Cells[row, 4].Text,
                            Car = worksheet.Cells[row, 5].Text,
                            Points = worksheet.Cells[row, 6].Text,
                            Diff = worksheet.Cells[row, 7].Text
                        };

                        excelDriverDataModels.Add(excelDriverDataModel);
                    }
                }
            }

            return excelDriverDataModels;
        }
    }
}