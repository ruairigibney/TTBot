using Discord;
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
        private readonly IEventAliasMapping _eventAliasMapping;
        private readonly IExcelSheetEventMapping _excelSheetEventMapping;

        public ChampionshipsModule(
            IChampionshipResults results,
            IPermissionService permissionService,
            IExcelService excelService,
            IEvents events,
            IEventAliasMapping eventAliasMapping,
            IExcelSheetEventMapping excelSheetEventMapping)
        {
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _eventAliasMapping = eventAliasMapping ?? throw new ArgumentNullException(nameof(eventAliasMapping));
            _excelSheetEventMapping = excelSheetEventMapping ?? throw new ArgumentNullException(nameof(excelSheetEventMapping));
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
                    }
                    else
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
                    e.LastRoundTrack = c.LastRoundTrack;
                    e.LastRoundDate = c.LastRoundDate;
                    await _events.SaveAsync(e);
                }


            }
            catch (Exception ex)
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

            var standingsChannelId = Context.Guild.Channels.First(c => c.Name.Contains("standings-wpr")).Id;
            var channel = Context.Guild.GetChannel(standingsChannelId) as IMessageChannel;
            await channel.SendMessageAsync(":medal:Standings have just been updated.");
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
            }
            catch (Exception ex)
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
            }
            else if (Regex.IsMatch(args, "top(3|5|10)$"))
            {
                var author = Context.Message.Author as SocketGuildUser;
                if (!await _permissionService.UserIsModeratorAsync(Context, author))
                {
                    await Context.Channel.SendMessageAsync("You dont have permission to display all standings");
                    return;
                }

                var topDriversToDisplay = Int32.Parse(args.Replace("top", ""));

                var championships = await _events.GetActiveEvents(guildId);
                foreach (var c in championships)
                {
                    await writeStandingsForChampionship(c.ShortName, guildId, topDriversToDisplay);
                }
            }
            else
            {
                await writeStandingsForChampionship(args, guildId);
            }
        }

        private async Task writeStandingsForChampionship(string alias, ulong guildId, int topDriversToDisplay = 0)
        {

            var sb = new StringBuilder();
            try
            {
                var aliasEvent = await _eventAliasMapping.GetActiveEventFromAliasAsync(alias, guildId);
                var activeEvent = await _events.GetActiveEvent(alias, guildId);
                if (aliasEvent == null && activeEvent == null)
                {
                    sb.AppendLine($"Championship alias {alias} not found");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                var eventId = aliasEvent != null ? aliasEvent.Id : activeEvent.Id;

                var e = await _events.GetActiveEvent(eventId);
                if (e == null || e.Id == 0)
                {
                    if (topDriversToDisplay == 0)
                    {
                        sb.AppendLine($"Championship {eventId} not found");
                        await ReplyAsync(sb.ToString());
                    }
                    return;
                }
                else
                {
                    var championship = e.ShortName;
                    var results = await _results.GetChampionshipResultsByIdAsync(eventId);
                    if (results.Count == 0)
                    {
                        return;
                    }

                    var orderedResults = results.OrderBy(r => r.Pos);

                    int posXStart = Utilities.OperatingSystem.IsWindows() ? 100 : 110;
                    int posYStart = 375;
                    int championshipX = Utilities.OperatingSystem.IsWindows() ? 245 : 252;
                    int championshipY = Utilities.OperatingSystem.IsWindows() ? 220 : 225;
                    int roundX = 660;
                    int roundY = 120;

                    var driverX = posXStart + 100;
                    var numberX = driverX + 400;
                    int pointsX = numberX + 110;
                    int diffX = pointsX + 155;

                    int lastRowY = 0;

                    string templateFilePath = @"Assets/StandingsTemplate.png";
                    using (Bitmap image = (Bitmap)System.Drawing.Image.FromFile(templateFilePath))
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
                        var championshipXMax = 370;
                        var championshipYMax = Utilities.OperatingSystem.IsWindows() ? 60 : 50;

                        // For testing - uncomment to show rectangles
                        /* Rectangle rect1 = new Rectangle(championshipX, championshipY, championshipXMax, championshipYMax);
                        graphics.FillRectangle(
                            new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 0)), rect1);
                        */

                        Size championshipSize = new Size(championshipXMax, championshipYMax);
                        graphics.DrawString(
                            championship,
                            graphics.GetAdjustedFont(championship, largerFont, championshipSize),
                            new SolidBrush(System.Drawing.Color.FromArgb(213, 213, 213)),
                            championshipX,
                            championshipY);

                        // write round details (if available)
                        if (e.Round != null && e.Round > 0)
                        {
                            graphics.DrawString(
                                $"Round {e.Round}",
                                largerFont,
                                new SolidBrush(System.Drawing.Color.FromArgb(213, 213, 213)),
                                roundX,
                                roundY);

                            graphics.DrawString(
                                e.LastRoundDate,
                                largerFont,
                                new SolidBrush(System.Drawing.Color.FromArgb(213, 213, 213)),
                                roundX,
                                roundY + 50);

                            var trackXMax = 310;
                            var trackYMax = Utilities.OperatingSystem.IsWindows() ? 60 : 40;
                            Size trackSize = new Size(trackXMax, trackYMax);

                            // For testing - uncomment to show rectangles
                            /*
                            Rectangle rect2 = new Rectangle(roundX, roundY + 100, trackXMax, trackYMax);
                            graphics.FillRectangle(
                                new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 0)), rect2);
                            */

                            graphics.DrawString(
                                e.LastRoundTrack,
                                graphics.GetAdjustedFont(e.LastRoundTrack, largerFont, trackSize),
                                new SolidBrush(System.Drawing.Color.FromArgb(213, 213, 213)),
                                roundX,
                                roundY + 100);
                        }

                        int y = Utilities.OperatingSystem.IsWindows() ? posYStart - 1 : posYStart - 4;
                        int driverPosition = 0;
                        foreach (ChampionshipResultsModel r in orderedResults)
                        {
                            if (topDriversToDisplay > 0 && driverPosition >= topDriversToDisplay)
                            {
                                break;
                            }

                            graphics.FillRoundedRectangle(
                                Brushes.White,
                                Utilities.OperatingSystem.IsWindows() ? posXStart + 7 : posXStart + 1,
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

                            driverPosition++;
                        }

                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            Bitmap imageToSave;

                            if (lastRowY + 75 < image.Height)
                            {
                                var imageCropRect = new Rectangle(0, 0, image.Width, lastRowY + 75);
                                imageToSave = image.Clone(imageCropRect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                            } else
                            {
                                imageToSave = image;
                            }

                            if (topDriversToDisplay > 0)
                            {
                                imageToSave = Draw.Resize(imageToSave, 25);
                            }

                            imageToSave.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

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

        [Command("alias")]
        [Alias("a")]
        public async Task Alias(string action = null, string aliases = null, string eventShortName = null)
        {
            var author = Context.Message.Author as SocketGuildUser;
            if (!await _permissionService.UserIsModeratorAsync(Context, author))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to create aliases");
                return;
            }
            var sb = new StringBuilder();
            if (action == null)
            {
                sb.AppendLine("Action missing");
                await ReplyAsync(sb.ToString());
                return;
            }

            var guildId = Context.Guild.Id;

            if (action == "add")
            {
                if (eventShortName == null || aliases == null)
                {
                    sb.AppendLine("Details missing for the add");
                    await ReplyAsync(sb.ToString());
                    return;
                }
                var e = await _events.GetActiveEvent(eventShortName, guildId);
                if (e == null)
                {
                    sb.AppendLine($"Unknown event with shortname {eventShortName}");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                var aliasesList = aliases.Split(',').Select(p => p.Trim()).ToList();

                foreach (var alias in aliasesList)
                {
                    if (await _eventAliasMapping.ActiveEventExistsAsync(alias, guildId))
                    {
                        sb.AppendLine($"Alias {alias} already exists on active event");
                    }
                    else
                    {
                        await _eventAliasMapping.AddAsync((ulong)e.Id, alias);
                        sb.AppendLine($"Alias {alias} added for event {eventShortName}");
                    }
                }
            }
            else if (action == "remove")
            {
                if (aliases == null)
                {
                    sb.AppendLine("Aliases missing for the remove");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                var aliasesList = aliases.Split(',').Select(p => p.Trim()).ToList();

                foreach (var alias in aliasesList)
                {
                    if (!await _eventAliasMapping.ActiveEventExistsAsync(alias, guildId))
                    {
                        sb.AppendLine($"Alias {alias} does not exist on an active event");
                    }
                    else
                    {
                        var activeEvent = await _eventAliasMapping.GetActiveEventFromAliasAsync(alias, guildId);
                        var aliasMappingId = await _eventAliasMapping.GetAliasIdAsync(alias, guildId);
                        await _eventAliasMapping.RemoveAsync(aliasMappingId);
                        sb.AppendLine($"Alias {alias} removed for active event {activeEvent.ShortName}");
                    }

                }
            }
            else if (action == "list")
            {
                var allActiveAliases = await _eventAliasMapping.GetAllActiveAliases();
                var allActiveEvents = await _events.GetActiveEvents(guildId);
                ulong eId = 0;

                foreach (var item in allActiveAliases.Select((value, i) => (value, i)))
                {
                    var em = item.value;
                    var index = item.i;

                    // only list active events
                    if (allActiveEvents.Where<Event>
                            (e => (ulong)e.Id == em.EventId).Select(e => e.ShortName).FirstOrDefault() == null)
                    {
                        continue;
                    }

                        if (eId != em.EventId)
                    {
                        if (eId > 0)
                        {
                            sb.Append('.');
                            sb.AppendLine();
                        }
                        sb.Append("***");
                        sb.Append(allActiveEvents.Where<Event>
                            (e => (ulong)e.Id == em.EventId).Select(e => e.ShortName).FirstOrDefault());
                        sb.Append("***");
                        sb.Append(": ");
                    }
                    else
                    {
                        sb.Append(", ");
                    }

                    sb.Append($"{em.Alias}");
                    eId = em.EventId;
                }
            } else
            {
                sb.AppendLine("Incorrect action - must be either add, remove or list");
            }

            await ReplyAsync(sb.ToString());
        }


        [Command("worksheets")]
        [Alias("w", "sheet", "sheets", "worksheet")]
        public async Task Sheets(string action = null, string worksheet = null, string eventShortName = null, bool isRoundsSheet = false)
        {
            var author = Context.Message.Author as SocketGuildUser;
            if (!await _permissionService.UserIsModeratorAsync(Context, author))
            {
                await Context.Channel.SendMessageAsync("You dont have permission to link worksheets");
                return;
            }
            var sb = new StringBuilder();
            if (action == null)
            {
                sb.AppendLine("Action missing");
                await ReplyAsync(sb.ToString());
                return;
            }

            var guildId = Context.Guild.Id;

            if (action == "add")
            {
                if (eventShortName == null || worksheet == null)
                {
                    sb.AppendLine("Details missing for the add");
                    await ReplyAsync(sb.ToString());
                    return;
                }
                var e = await _events.GetActiveEvent(eventShortName, guildId);
                if (e == null)
                {
                    sb.AppendLine($"Unknown event with shortname {eventShortName}");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                if (await _excelSheetEventMapping.ActiveEventExistsAsync(worksheet))
                {
                    sb.AppendLine($"Sheetname {worksheet} already exists on active event");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                await _excelSheetEventMapping.AddAsync((ulong)e.Id, worksheet, isRoundsSheet);
                sb.AppendLine($"Worksheet {worksheet} added for event {eventShortName}");
                await ReplyAsync(sb.ToString());
                return;
            }
            else if (action == "remove")
            {
                if (worksheet == null)
                {
                    sb.AppendLine("Alias missing for the remove");
                    await ReplyAsync(sb.ToString());
                    return;
                }
                if (!await _excelSheetEventMapping.ActiveEventExistsAsync(worksheet))
                {
                    sb.AppendLine($"Worksheet {worksheet} does not exist on an active event");
                    await ReplyAsync(sb.ToString());
                    return;
                }

                var activeEvent = await _excelSheetEventMapping.GetActiveEventFromWorksheetAsync(worksheet);
                var worksheetMappingId = await _excelSheetEventMapping.GetWorksheetMappingIdAsync(worksheet);

                await _excelSheetEventMapping.RemoveAsync(worksheetMappingId);
                sb.AppendLine($"Worksheet {worksheet} removed for active event {activeEvent.ShortName}");
                await ReplyAsync(sb.ToString());
                return;
            }
            else if (action == "list")
            {
                var allActiveWorksheets = await _excelSheetEventMapping.GetAllActiveWorksheetMappings();
                var allActiveEvents = await _events.GetActiveEvents(guildId);
                ulong eId = 0;

                foreach (var item in allActiveWorksheets.Select((value, i) => (value, i)))
                {
                    var w = item.value;
                    var index = item.i;

                    // only list active events
                    if (allActiveEvents.Where<Event>
                            (e => (ulong)e.Id == w.EventId).Select(e => e.ShortName).FirstOrDefault() == null)
                    {
                        continue;
                    }

                    if (eId != w.EventId)
                    {
                        if (eId > 0)
                        {
                            sb.Append('.');
                            sb.AppendLine();
                        }
                        sb.Append("***");
                        sb.Append(allActiveEvents.Where<Event>
                            (e => (ulong)e.Id == w.EventId).Select(e => e.ShortName).FirstOrDefault());
                        sb.Append("***");
                        sb.Append(": ");
                    }
                    else
                    {
                        sb.Append(", ");
                    }

                    sb.Append($"{w.Sheetname}");
                    var sheetType = w.IsRoundsSheet ? "R" : "S";
                    sb.Append($" *({sheetType})*");
                    eId = w.EventId;
                }
                await ReplyAsync(sb.ToString());
                return;
            } else
            {
                sb.AppendLine("Incorrect action - must be either add, remove or list");
            }

            await ReplyAsync(sb.ToString());
        }

        [Command("help")]
        public async Task Help()
        {
            await Context.Channel.SendMessageAsync("Use `!championships list` to see a list of all championships with standings. To get the standings for a specific championship, " +
                "use `!championships standings` command with the name of the championship. For example `!championships standings MX5`.");
        }
    }


}
