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
            public int race_time_s;
            public int lap_time_ms;
            public double fuel_usage_l;

            public int reserve_laps;

            public int race_laps;
            public double fuel_per_minute;

            public double fuel;
            public double fuel_save;



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
                    x.Value = TimeSpan.FromSeconds(data.race_time_s).ToString(@"hh\:mm");
                    x.IsInline = true;
                });

                builder.AddField(x =>
                {
                    x.Name = "Laptime";
                    x.Value = TimeSpan.FromMilliseconds(data.lap_time_ms).ToString(@"mm\:ss\.fff");
                    x.IsInline = true;
                });


                builder.AddField(x =>
                {
                    x.Name = "Fuel per Lap";
                    x.Value = data.fuel_usage_l;
                    x.IsInline = true;
                });

                builder.AddField(x =>
                {
                    x.Name = "Racelaps";
                    x.Value = data.race_laps;
                    x.IsInline = true;
                });

                builder.AddField(x =>
                {
                    x.Name = "Fuel per Minute (est.)";
                    x.Value = data.fuel_per_minute.ToString("0.00");
                    x.IsInline = true;
                });

                builder.AddField(x =>
                {
                    x.Name = "Fuel (min)";
                    x.Value = data.fuel.ToString("0.00");
                    x.IsInline = false;
                });


                double reserve_laps_perc = data.reserve_laps / (double)data.race_laps * 100;
                builder.AddField(x =>
                {
                    x.Name = $"Fuel (+{data.reserve_laps} laps / +{(int)reserve_laps_perc}%)";
                    x.Value = data.fuel_save.ToString("0.00");
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
        private TimeSpan parse_racelen(string timestamp)
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
        private TimeSpan parse_laptime(string timestamp)
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




        private RaceFuel get_fuel_usage(RaceFuel data)
        {

            data.fuel_per_minute = data.fuel_usage_l * 1000 / data.lap_time_ms * 60;

            data.fuel = data.race_laps * data.fuel_usage_l;
            data.fuel_save = data.fuel + (data.reserve_laps * data.fuel_usage_l);

            return data;
        }



        [Command("")]
        [Alias("time")]
        [Summary("Calculate fuel usage by time")]
        public async Task FuelTime(string race_len, string lap_time, string fuel_usage, int reserve_laps = 3)
        {

            RaceFuel data = new RaceFuel
            {
                race_time_s = (int)parse_racelen(race_len).TotalSeconds,
                lap_time_ms = (int)parse_laptime(lap_time).TotalMilliseconds,
                fuel_usage_l = double.Parse(fuel_usage, CultureInfo.InvariantCulture),
                reserve_laps = reserve_laps
            };


            /* calculating race laps here, as called function is generic and requires this value */

            double race_laps_d = data.race_time_s * 1000 / (double)data.lap_time_ms;
            data.race_laps = (int)Math.Ceiling(race_laps_d);



            data = get_fuel_usage(data);
            await Context.Channel.SendMessageAsync(embed: data.toEmbed(Context.Client.CurrentUser, Context.User));
            
        }

        
        [Command("laps")]
        [Summary("Calculate fuel usage by laps")]
        public async Task FuelLaps(int race_laps, string lap_time, string fuel_usage, int reserve_laps = 3)
        {
            RaceFuel data = new RaceFuel
            {
                race_laps = race_laps,
                lap_time_ms = (int)parse_laptime(lap_time).TotalMilliseconds,
                fuel_usage_l = double.Parse(fuel_usage, CultureInfo.InvariantCulture),
                reserve_laps = reserve_laps
            };


            /* calculating race time here, as called function is generic and requires this value */

            data.race_time_s = data.race_laps * data.lap_time_ms / 1000;


            data = get_fuel_usage(data);
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
