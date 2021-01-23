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
                    if (worksheet.Dimension == null)
                    {
                        continue;
                    }

                    int rowCount = worksheet.Dimension.End.Row;
                    int colCount = worksheet.Dimension.End.Column;

                    for (int i = 0; i < rowCount; i++)
                    {
                        var row = i + 1;
                        ExcelDriverDataModel excelDriverDataModel = new ExcelDriverDataModel()
                        {
                            Championship = worksheet.Name,
                            Driver = worksheet.Cells[row, 1].Text,
                            Positions = new string[colCount - 1]
                        };

                        for (int ii = 0; ii < colCount - 1; ii++)
                        {
                            var col = ii + 2;
                            excelDriverDataModel.Positions[ii] = worksheet.Cells[row, col].Text;
                        }

                        excelDriverDataModels.Add(excelDriverDataModel);
                    }
                }
            }

            return excelDriverDataModels;
        }
    }
}