using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Primitives;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using TTBot.DataAccess;
using TTBot.Services;

namespace TTBot.Commands
{
    [Group("fuel")]
    public class FuelModule : ModuleBase<SocketCommandContext>
    {
        struct RaceFuel
        {
            public int raceTimeS;
            public int lapTimeMs;
            public double fuelUsageL;

            public int reserveLaps;

            public int raceLaps;
            public double fuelPerMinute;

            public double fuel;
            public double fuelSave;



            /// <summary>
            /// Create an embed which visualised this struct
            /// </summary>
            /// <param name="bot">optional for Author header</param>
            /// <param name="user">optional for Footer text</param>
            /// <returns></returns>
            public Embed toEmbed(SocketSelfUser bot = null,  SocketUser user = null)
            {
                /* this cannot be used in anonymous expressions */
                RaceFuel data = this;


                var builder = new EmbedBuilder()
                {
                    Color = Color.Blue,
                    Title = "Fuel Calculation",
                };
                


                builder.AddField(x =>
                {
                    x.Name = "Racetime";
                    x.Value = TimeSpan.FromSeconds(data.raceTimeS).ToString(@"hh\:mm");
                    x.IsInline = true;
                });

                builder.AddField(x =>
                {
                    x.Name = "Laptime";
                    x.Value = TimeSpan.FromMilliseconds(data.lapTimeMs).ToString(@"mm\:ss\.fff");
                    x.IsInline = true;
                });


                builder.AddField(x =>
                {
                    x.Name = "Fuel per Lap";
                    x.Value = data.fuelUsageL;
                    x.IsInline = true;
                });

                builder.AddField(x =>
                {
                    x.Name = "Racelaps";
                    x.Value = data.raceLaps;
                    x.IsInline = true;
                });

                builder.AddField(x =>
                {
                    x.Name = "Fuel per Minute (est.)";
                    x.Value = data.fuelPerMinute.ToString("0.00");
                    x.IsInline = true;
                });

                builder.AddField(x =>
                {
                    x.Name = "Fuel (min)";
                    x.Value = data.fuel.ToString("0.00");
                    x.IsInline = false;
                });


                double reserve_laps_perc = data.reserveLaps / (double)data.raceLaps * 100;
                builder.AddField(x =>
                {
                    x.Name = $"Fuel (+{data.reserveLaps} laps / +{(int)reserve_laps_perc}%)";
                    x.Value = data.fuelSave.ToString("0.00");
                    x.IsInline = false;
                });


                


                if (bot != null)
                {
                    builder.WithAuthor(a =>
                    {
                        a.Name = bot.Username;
                        a.IconUrl = $"https://cdn.discordapp.com/avatars/{bot.Id}/{bot.AvatarId}.png";
                    });
                }


                if (user is SocketGuildUser gUser)
                {
                    string uName = (gUser.Nickname != null) ? gUser.Nickname : gUser.Username;

                    builder.WithFooter(f =>
                    {
                        f.Text = $"Calculation requested by {uName}";
                        f.IconUrl = $"https://cdn.discordapp.com/avatars/{gUser.Id}/{gUser.AvatarId}.png";
                    });
                }

                return builder.Build();
            }

        };



        public FuelModule()
        {
            
        }



        /// <summary>
        /// Convert a string to TimeSpan
        /// Convert priority:
        ///     1. hh:mm
        ///     2. h:mm
        ///     3. [minutes]
        ///     4. return 0 seconds
        /// 
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns>0 TimeSpan on failure</returns>
        private TimeSpan ParseRaceLen(string timestamp)
        {
            TimeSpan ts;


            if (TimeSpan.TryParseExact(timestamp, @"hh\:mm", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }

            if (TimeSpan.TryParseExact(timestamp, @"h\:mm", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }


            try
            {
                return TimeSpan.FromMinutes(double.Parse(timestamp, CultureInfo.InvariantCulture));
            }
            catch
            {
                return new TimeSpan();
            }
        }



        /// <summary>
        /// Convert a string to TimeSpan
        /// Convert priority:
        ///     1. m:ss.ffff
        ///     2. m:ss.fff
        ///     3. m:ss.ff
        ///     4. m:ss.f
        ///     5. m:ss
        ///     6. mm:ss
        ///     3. [total seconds]
        ///     4. return 0 seconds
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns>0 TimeSpan on failure</returns>
        private TimeSpan ParseLaptime(string timestamp)
        {
            TimeSpan ts;

            /* parsing TimeSpan is quiet annoying */


            if (TimeSpan.TryParseExact(timestamp, @"m\:ss\.ffff", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }

            if (TimeSpan.TryParseExact(timestamp, @"m\:ss\.fff", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }

            if (TimeSpan.TryParseExact(timestamp, @"m\:ss\.ff", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }

            if (TimeSpan.TryParseExact(timestamp, @"m\:ss\.f", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }

            if (TimeSpan.TryParseExact(timestamp, @"m\:ss", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }

            if (TimeSpan.TryParseExact(timestamp, @"mm\:ss", CultureInfo.InvariantCulture, out ts))
            {
                return ts;
            }


            try
            {
                return TimeSpan.FromSeconds(double.Parse(timestamp, CultureInfo.InvariantCulture));
            }
            catch
            {
                return new TimeSpan();
            }
        }




        private RaceFuel GetFuelUsage(RaceFuel data)
        {

            data.fuelPerMinute = data.fuelUsageL * 1000 / data.lapTimeMs * 60;

            data.fuel = data.raceLaps * data.fuelUsageL;
            data.fuelSave = data.fuel + (data.reserveLaps * data.fuelUsageL);

            return data;
        }



        [Command("")]
        [Alias("time")]
        [Summary("Calculate fuel usage by time")]
        public async Task FuelTime(string race_len, string lap_time, string fuel_usage, int reserve_laps = 3)
        {

            RaceFuel data = new RaceFuel
            {
                raceTimeS = (int)ParseRaceLen(race_len).TotalSeconds,
                lapTimeMs = (int)ParseLaptime(lap_time).TotalMilliseconds,
                fuelUsageL = double.Parse(fuel_usage, CultureInfo.InvariantCulture),
                reserveLaps = reserve_laps
            };


            /* calculating race laps here, as called function is generic and requires this value */

            double race_laps_d = data.raceTimeS * 1000 / (double)data.lapTimeMs;
            data.raceLaps = (int)Math.Ceiling(race_laps_d);



            data = GetFuelUsage(data);
            await Context.Channel.SendMessageAsync(embed: data.toEmbed(Context.Client.CurrentUser, Context.User));
            
        }

        
        [Command("laps")]
        [Summary("Calculate fuel usage by laps")]
        public async Task FuelLaps(int race_laps, string lap_time, string fuel_usage, int reserve_laps = 3)
        {
            RaceFuel data = new RaceFuel
            {
                raceLaps = race_laps,
                lapTimeMs = (int)ParseLaptime(lap_time).TotalMilliseconds,
                fuelUsageL = double.Parse(fuel_usage, CultureInfo.InvariantCulture),
                reserveLaps = reserve_laps
            };


            /* calculating race time here, as called function is generic and requires this value */

            data.raceTimeS = data.raceLaps * data.lapTimeMs / 1000;


            data = GetFuelUsage(data);
            await Context.Channel.SendMessageAsync(embed: data.toEmbed(Context.Client.CurrentUser, Context.User));
        }



        [Command("help")]
        public async Task Help()
        {
            await Context.Channel.SendMessageAsync("You can calculate an estimate for your fuel usage. use `!fuel <race length> <laptime> <fuel per lap>` to trigger a calculation. If your race is distance limited, use `!fuel laps <laps> <laptime> <fuel pre lap>`");
            await Context.Channel.SendMessageAsync("You can specify the number of save-laps as an optional 4th argument");
        }
    }


}
