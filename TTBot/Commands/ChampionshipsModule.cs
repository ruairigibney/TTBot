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
    [Alias("c", "champ", "champs", "championship")]
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

                    // clear the Round column for each championship imported
                    // (it will be populated later and we want to avoid stale data)
                    e.Round = 0;
                    await _events.SaveAsync(e);

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

                var derivedRoundsForChampionships = await _excelService.DeriveRoundsFromAttachment(attachment);
                foreach (var c in derivedRoundsForChampionships)
                {
                    var e = await _events.GetActiveEvent(c.Championship, guildId);

                    if (e == null || e.Id == 0)
                    {
                        continue;
                    }

                    e.Round = c.Round;
                    await _events.SaveAsync(e);
                }


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
        [Alias("l")]
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
        [Alias("s")]
        public async Task GetStandings([Remainder] string args = null)
        {
            var guildId = Context.Guild.Id;
            var sb = new StringBuilder();

            if (args == null)
            {
                sb.AppendLine("No championship provided");
                await ReplyAsync(sb.ToString());
                return;
            } else if (args == "all")
            {
                var author = Context.Message.Author as SocketGuildUser;
                if (!await _permissionService.UserIsModeratorAsync(Context, author))
                {
                    await Context.Channel.SendMessageAsync("You dont have permission to display all standings");
                    return;
                }

                var championships = await _events.GetActiveEvents(guildId);
                foreach (var c in championships)
                {
                    await writeStandingsForChampionship(c.ShortName, guildId);
                }
            } else
            {
                await writeStandingsForChampionship(args, guildId);
            }
        }

        private async Task writeStandingsForChampionship(string alias, ulong guildId)
        {

            var sb = new StringBuilder();
            try
            {
                var championship = ChampionshipAliasesModel.GetEventShortnameFromAlias(alias);
                if (championship == null)
                {
                    sb.AppendLine($"Championship alias {alias} not found");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                var e = await _events.GetActiveEvent(championship, guildId);
                if (e == null || e.Id == 0)
                {
                    sb.AppendLine($"Championship {championship} not found");
                    await ReplyAsync(sb.ToString());
                    return;
                }
                else
                {
                    var eventId = e.Id;
                    var results = await _results.GetChampionshipResultsByIdAsync(eventId);
                    if (results.Count == 0)
                    {
                        return;
                    }

                    var orderedResults = results.OrderBy(r => r.Pos);

                    int posXStart = Utilities.OperatingSystem.IsWindows() ? 100 : 110;
                    int posYStart = 375;
                    int championshipX = 250;
                    int championshipY = 230;
                    int roundX = 700;
                    int roundY = 210;

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
                        fontCol.AddFontFile(@"Assets/Fonts/Formula1-Regular.otf");
                        var formula1FontFamily = fontCol.Families[0];

                        Font font, numberFont, longDriverFont, largerFont;
                        if (Utilities.OperatingSystem.IsWindows())
                        {
                            font = new Font(formula1FontFamily, 8);
                            numberFont = new Font(formula1FontFamily, 7);
                            longDriverFont = new Font(formula1FontFamily, 5);
                            largerFont = new Font(formula1FontFamily, 10);
                        }
                        else
                        {
                            font = new Font(formula1FontFamily.Name, 24);
                            numberFont = new Font(formula1FontFamily.Name, 20);
                            longDriverFont = new Font(formula1FontFamily.Name, 18);
                            largerFont = new Font(formula1FontFamily.Name, 28);
                        }
                        
                        // write championship
                        Size championshipSize = new Size(120, 200);
                        graphics.DrawString(
                            championship,
                            graphics.GetAdjustedFont(championship, largerFont, championshipSize),
                            new SolidBrush(Color.FromArgb(213, 213, 213)),
                            championshipX,
                            championshipY);; ;

                        // write round (if it's available)
                        if (e.Round > 0)
                        {
                            graphics.DrawString(
                                $"Round {e.Round}",
                                largerFont,
                                new SolidBrush(Color.FromArgb(213, 213, 213)),
                                roundX,
                                roundY);
                        }

                        int y = Utilities.OperatingSystem.IsWindows() ? posYStart : posYStart - 4;
                        foreach (ChampionshipResultsModel r in orderedResults)
                        {
                            graphics.FillRoundedRectangle(
                                Brushes.White,
                                Utilities.OperatingSystem.IsWindows() ? posXStart + 7: posXStart + 1,
                                Utilities.OperatingSystem.IsWindows() ? y - 3 : y - 5,
                                50,
                                40,
                                4);

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

                        font.Dispose(); numberFont.Dispose(); longDriverFont.Dispose(); largerFont.Dispose();
                    }
                }
            }
            catch (Exception ex)
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
