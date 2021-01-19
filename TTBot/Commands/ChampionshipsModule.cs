using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using TTBot.DataAccess;
using TTBot.Models;
using TTBot.Services;

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

                // clear out our results DB table first
                await _results.DeleteAllAsync<ChampionshipResultsModel>();

                var attachment = Context.Message.Attachments.First();
                var excelData = await _excelService.ReadResultsDataFromAttachment(attachment);

                List<ChampionshipResultsModel> championshipResults = new List<ChampionshipResultsModel>();

                foreach (ExcelDataModel excelDataModel in excelData)
                {
                    var e = await _events.GetEventByShortname(excelDataModel.Championship);

                    if (e == null || e.Id == 0)
                    {
                        if (!listOfUnknownChampionships.Contains(excelDataModel.Championship))
                        {
                            listOfUnknownChampionships.Add(excelDataModel.Championship);
                        }
                        continue;
                    } else
                    {
                        if (!listOfSuccessfulyUploadedChampionships.Contains(excelDataModel.Championship))
                        {
                            listOfSuccessfulyUploadedChampionships.Add(excelDataModel.Championship);
                        }
                    }

                    var eventId = e.Id;
                    var points = 0;

                    foreach (String position in excelDataModel.Positions)
                    {
                        int p;
                        if (position.Contains("*"))
                        {
                            points += ChampionshipPointsModel.FastestLapPoints;
                            p = Int32.Parse(position.Remove(position.IndexOf("*"), 1));
                        }
                        else
                        {
                            p = Int32.Parse(position);
                        }

                        points += ChampionshipPointsModel.PointsAwarded[p];
                    }

                    ChampionshipResultsModel championshipResult = new ChampionshipResultsModel()
                    {
                        EventId = eventId,
                        Driver = excelDataModel.Driver,
                        Positions = excelDataModel.Positions,
                        Points = points
                    };

                    championshipResults.Add(championshipResult);

                }

                _results.AddAsync(championshipResults);


            } catch (Exception ex)
            {
                sb.AppendLine($"Error when doing import: {ex.Message}");
            }

            if (listOfSuccessfulyUploadedChampionships.Count > 0)
            {
                sb.AppendLine($"Data has been imported for the following championships: {string.Join(",", listOfSuccessfulyUploadedChampionships)}");
            }

            if (listOfUnknownChampionships.Count > 0)
            {
                sb.AppendLine($"The following championship shortnames were not found in events: {string.Join(",", listOfUnknownChampionships)}");
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
        public async Task GetStandings(string args = null)
        {
            var championship = args;

            var sb = new StringBuilder();

            try
            {
                var e = await _events.GetEventByShortname(championship);
                if (e == null || e.Id == 0)
                {
                    sb.AppendLine("Championship not found");
                    await ReplyAsync(sb.ToString());
                    return;
                } else
                {
                    var eventId = e.Id;
                    var results = await _results.GetChampionshipResultsByIdAsync(eventId);
                    var orderedResults = results.OrderByDescending(r => r.Points);

                    var pos = 1;
                    foreach (ChampionshipResultsModel r in orderedResults)
                    {
                        sb.AppendLine($"#{pos}: {r.Driver}: {r.Points}");
                        pos++;
                    }
                }
            } catch (Exception ex)
            {
                sb.AppendLine($"Error when getting standings: {ex.Message}");
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
