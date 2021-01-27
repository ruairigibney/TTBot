using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
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

                    var resultsTable = orderedResults.ToStringTable(
                        new[] { "Pos", "Driver", "Number", "Car", "Points", "Diff"},
                            r => r.Pos, r => r.Driver, r => r.Number, r => r.Car, r => r.Points, r => r.Diff);

                    if (resultsTable.Length >= 2000)
                    {
                        // split in to multiple messages
                        using (System.IO.StringReader reader = new System.IO.StringReader(resultsTable))
                        {
                            string line = null;
                            int i = -2;
                            sb.AppendLine("```");
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (i > 0 && i%10 == 0)
                                {
                                    sb.AppendLine("```");
                                }
                                sb.AppendLine(line);
                                if (i % 10 == 9)
                                {
                                    sb.AppendLine("```");
                                    await ReplyAsync(sb.ToString());
                                    sb = new StringBuilder();
                                }
                                i++;
                            }
                        }
                    } else
                    {
                        sb.AppendLine("```");
                        sb.AppendLine(resultsTable);
                        sb.AppendLine("```");
                        await ReplyAsync(sb.ToString());
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
