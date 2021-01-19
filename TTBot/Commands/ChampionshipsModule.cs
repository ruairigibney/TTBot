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
        private readonly IChampionships _championships;

        public ChampionshipsModule(
            IChampionshipResults results,
            IPermissionService permissionService,
            IExcelService excelService,
            IChampionships championships)
        {
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _championships = championships ?? throw new ArgumentNullException(nameof(championships));
        }

        [Command("import")]
        public async Task Standings()
        {
            var sb = new StringBuilder();

            try
            {
                var attachment = Context.Message.Attachments.First();

                var excelData = await _excelService.ReadResultsDataFromAttachment(attachment);

                List<ChampionshipResultsModel> championshipResults = new List<ChampionshipResultsModel>();

                foreach (ExcelDataModel excelDataModel in excelData)
                {
                    var championshipId = await _championships.Exists(excelDataModel.Championship);

                    if (championshipId == 0)
                    {
                        championshipId = await _championships.AddAsync(excelDataModel.Championship);
                    }

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
                        ChampionshipId = championshipId,
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

            sb.AppendLine("Data has been imported successfully");

            await ReplyAsync(sb.ToString());
        }

        [Command("list")]
        public async Task GetChampionships()
        {

            var sb = new StringBuilder();

            try
            {
                var championships = _championships.GetAllAsync().Result;

                sb.AppendLine("Standings currently available for: ");
                sb.AppendLine("");

                foreach (Championship championship in championships)
                {
                    sb.AppendLine($" - {championship.Name}");
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
                var championshipId = await _championships.Exists(championship);
                if (championshipId == 0)
                {
                    return;
                } else
                {
                    // DOESN'T WORK YET
                    var results = _results.GetChampionshipResultsById(championshipId).Result.OrderByDescending(r => r.Points);

                    var i = 1;
                    foreach (ChampionshipResultsModel result in results)
                    {
                        sb.AppendLine($"#{i + 1}: {result.Driver}: {result.Points}");
                        i++;
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
            await Context.Channel.SendMessageAsync("TODO");
        }
    }


}
