using Discord;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using TTBot.Models;

namespace TTBot.Services
{
    public class ExcelWrapper : IExcelWrapper
    {
        public ExcelWrapper()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public List<ExcelDriverDataModel> GetExcelDriverData(Attachment attachment)
        {
            List<ExcelDriverDataModel> excelDriverDataModels = new List<ExcelDriverDataModel>();

            WebClient webClient = new WebClient();
            byte[] buffer = webClient.DownloadData(attachment.Url);
            using MemoryStream stream = new MemoryStream(buffer);
            using (var package = new ExcelPackage(stream))
            {
                foreach (ExcelWorksheet worksheet in package.Workbook.Worksheets)
                {
                    if (worksheet.Dimension == null ||
                        !ExcelSheetShortnameMappingModel.mappings.ContainsKey(worksheet.Name))
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
                            Championship = ExcelSheetShortnameMappingModel.mappings[worksheet.Name],
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