using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TTBot.DataAccess;
using TTBot.Models;
using TTBot.Services;
using TTBot.Utilities;

namespace TTBot.Commands
{
    [Group("championships")]
    public class ChampionshipsModule : ModuleBase<SocketCommandContext>
    {
        private readonly IChampionshipResults _results;
        private readonly IPermissionService _permissionService;
        private readonly IExcelService _excelService;
        private readonly IEvents _events;

        public ChampionshipsModule(
            IChampionshipResults results,
            IPermissionService permissionService,
            IExcelService excelService,
            IEvents events)
        {
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        [Command("import")]
        public async Task Standings()
        {
            var sb = new StringBuilder();

            List<string> listOfUnknownChampionships = new List<string>();
            List<string> listOfSuccessfulyUploadedChampionships = new List<string>();

            try
            {
                var author = Context.Message.Author as SocketGuildUser;
                if (!await _permissionService.UserIsModeratorAsync(Context, author))
                {
                    await Context.Channel.SendMessageAsync("You dont have permission to create events");
                    return;
                }

                var guildId = Context.Guild.Id;

                // clear out our results DB table first
                await _results.DeleteAllGuildEvents<ChampionshipResultsModel>(guildId.ToString());

                var attachment = Context.Message.Attachments.First();
                var excelDriverDataModels = await _excelService.ReadResultsDataFromAttachment(attachment);

                List<ChampionshipResultsModel> championshipResults = new List<ChampionshipResultsModel>();

                foreach (ExcelDriverDataModel excelDriverDataModel in excelDriverDataModels)
                {
                    var e = await _events.GetActiveEvent(excelDriverDataModel.Championship, guildId);

                    if (e == null || e.Id == 0)
                    {
                        if (!listOfUnknownChampionships.Contains(excelDriverDataModel.Championship))
                        {
                            listOfUnknownChampionships.Add(excelDriverDataModel.Championship);
                        }
                        continue;
                    } else
                    {
                        if (!listOfSuccessfulyUploadedChampionships.Contains(excelDriverDataModel.Championship))
                        {
                            listOfSuccessfulyUploadedChampionships.Add(excelDriverDataModel.Championship);
                        }
                    }

                    var eventId = e.Id;

                    ChampionshipResultsModel championshipResult = new ChampionshipResultsModel()
                    {
                        EventId = eventId,
                        Pos = excelDriverDataModel.Pos,
                        Driver = excelDriverDataModel.Driver,
                        Number = excelDriverDataModel.Number,
                        Car = excelDriverDataModel.Car,
                        Points = excelDriverDataModel.Points,
                        Diff = excelDriverDataModel.Diff

                    };

                    championshipResults.Add(championshipResult);

                }

                await _results.AddAsync(championshipResults);


            } catch (Exception ex)
            {
                sb.AppendLine($"Error when doing import: {ex.Message}");
            }

            if (listOfSuccessfulyUploadedChampionships.Count > 0)
            {
                sb.AppendLine($"Data has been imported for the following championships: {string.Join(", ", listOfSuccessfulyUploadedChampionships)}");
            }

            if (listOfUnknownChampionships.Count > 0)
            {
                sb.AppendLine($"The following championship shortnames were not found in events: {string.Join(", ", listOfUnknownChampionships)}");
            }

            await ReplyAsync(sb.ToString());
        }

        [Command("list")]
        public async Task GetChampionships()
        {

            var sb = new StringBuilder();

            try
            {
                var events = await _results.GetEventsWithResultsAsync();

                if (events.Length == 0)
                {
                    sb.AppendLine("No standings currently available");
                }
                else
                {
                    sb.AppendLine("Standings currently available for: ");
                    sb.AppendLine("");

                    foreach (string e in events)
                    {
                        sb.AppendLine($" - {e}");
                    }
                }
            } catch (Exception ex)
            {
                sb.AppendLine($"Error when doing list: {ex.Message}");
            }

            await ReplyAsync(sb.ToString());
        }

        [Command("standings")]
        public async Task GetStandings([Remainder] string args = null)
        {
            var championship = args;
            var guildId = Context.Guild.Id;

            var sb = new StringBuilder();

            if (args == null)
            {
                sb.AppendLine("No championship provided");
                await ReplyAsync(sb.ToString());
                return;
            }

            try
            {
                var e = await _events.GetActiveEvent(championship, guildId);
                if (e == null || e.Id == 0)
                {
                    sb.AppendLine("Championship not found");
                    await ReplyAsync(sb.ToString());
                    return;
                } else
                {
                    var eventId = e.Id;
                    var results = await _results.GetChampionshipResultsByIdAsync(eventId);
                    var orderedResults = results.OrderBy(r => r.Pos);

                    var posXStart = 100;
                    int posYStart = 375;
                    int championshipX = 250;
                    int championshipY = 230;

                    var driverX = posXStart + 100;
                    var numberX = driverX + 400;
                    int pointsX = numberX + 110;
                    int diffX = pointsX + 155;

                    int lastRowY = 0;

                    string templateFilePath = @"Assets/StandingsTemplate.png";
                    using (Bitmap image = (Bitmap)Image.FromFile(templateFilePath))
                    using (Graphics graphics = Graphics.FromImage(image))
                    {
                        PrivateFontCollection fontCol = new PrivateFontCollection();
                        fontCol.AddFontFile(@"Assets\Formula1-Regular.otf");
                        var formula1FontFamily = fontCol.Families[0];

                        using (Font font = new Font(formula1FontFamily, 7))
                        using (Font numberFont = new Font(formula1FontFamily, 6))
                        using (Font longDriverFont = new Font(formula1FontFamily, 5))
                        using (Font championshipFont = new Font(formula1FontFamily, 10))
                        using (Font smallerChampionshipFont = new Font(formula1FontFamily, 7))
                        {
                            graphics.DrawString(
                                championship,
                                championship.Trim().Length < 10 ? championshipFont : smallerChampionshipFont,
                                new SolidBrush(Color.FromArgb(213, 213, 213)),
                                championshipX,
                                championshipY);

                            int y = posYStart;
                            foreach (ChampionshipResultsModel r in orderedResults)
                            {
                                graphics.FillRoundedRectangle(Brushes.White, posXStart, y - 5, 60, 40, 4);

                                var posX = r.Pos <= 9
                                    ? posXStart + 15
                                    : posXStart + 5;

                                graphics.DrawString(r.Pos.ToString(), numberFont, Brushes.Black, posX, y);
                                graphics.DrawString(
                                    r.Driver,
                                    r.Driver.Length <= 25 ? font : longDriverFont,
                                    Brushes.White,
                                    driverX,
                                     r.Driver.Length <= 25 ? y : y + 6);
                                graphics.DrawString(r.Number, font, Brushes.White, numberX, y);
                                graphics.DrawString(r.Points, font, Brushes.White, pointsX, y);
                                graphics.DrawString(r.Diff, font, Brushes.White, diffX, y);

                                lastRowY = y;

                                y += 50;
                            }
                        }

                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            if (lastRowY + 75 < image.Height)
                            {
                                var imageCropRect = new Rectangle(0, 0, image.Width, lastRowY + 75);
                                image.Clone(imageCropRect, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                                    .Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                            }
                            else
                            {
                                image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                            }

                            memoryStream.Position = 0;
                            await Context.Channel.SendFileAsync
                                (memoryStream, $"{championship}-standings-{DateTime.Now.ToString("yyyy-dd-M-HH-mm-ss")}.png");
                        }
                    }
                }
            } catch (Exception ex)
            {
                sb.AppendLine($"Error when getting standings: {ex.Message}");
                await ReplyAsync(sb.ToString());
            }
        }

        [Command("help")]
        public async Task Help()
        {
            await Context.Channel.SendMessageAsync("Use `!championships list` to see a list of all championships with standings. To get the standings for a specific championship, " + 
                "use `!championships standings` command with the name of the championship. For example `!championships standings MX5`.");
        }
    }


}
