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
                    string lastTrack = "", lastDate = "";

                    for (int c = 1; c < colCount; c++)
                    {
                        // find next round col
                        while (!worksheet.Cells[1, c].Text.ToLower().Contains("round") && c < colCount)
                        {
                            c++;
                        }

                        if (worksheet.Cells[1, c].Text.ToLower().Contains("round")) {
                            var roundBeingRead = worksheet.Cells[1, c].Text.ToLower().Replace("round", "").Trim();
                            var dateBeingRead = worksheet.Cells[2, c].Text;
                            var trackBeingRead = worksheet.Cells[3, c].Text;

                            for (int r = 1; r < colCount; r++)
                            {
                                var row = r + 5;

                                if (!string.IsNullOrWhiteSpace(worksheet.Cells[row, c].Text) &&
                                    int.Parse(worksheet.Cells[row, c].Text) > 0) {
                                    maxRound = int.Parse(roundBeingRead);
                                    lastDate = dateBeingRead;

                                    var lastTrackArray = trackBeingRead.Split();
                                    lastTrack = lastTrackArray[0] + " " +
                                        (lastTrackArray.Length > 1 ? lastTrackArray[1] : "");

                                    c++; // skip this column, so we can start looking for the next col
                                    break;
                                }
                            }
                        }
                    }

                    excelChampionshipRounds.Add(new ExcelChampionshipRoundModel()
                    {
                        Championship = eventShortname,
                        Round = maxRound,
                        LastRoundDate = lastDate,
                        LastRoundTrack = lastTrack
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
                        if (string.IsNullOrWhiteSpace(worksheet.Cells[row, 3].Text)
                            || worksheet.Cells[row, 3].Text == "0")
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