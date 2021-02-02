using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TTBot.Models
{
    public class ChampionshipAliasesModel
    {
        private static Dictionary<string, string[]> aliasesDictionary = new Dictionary<string, string[]>() {
            { "ACC GT3 8pm", new string[]{ "ACC Pro", "Pro Am", "Pro/Am", "GT Pro", "Mon Pro", "Monday Pro" } },
            { "ACC GT3 7.30pm", new string[]{ "ACC Beg", "Beg", "GT3 Beg", "Mon Beg", "Monday Beg" } },
            { "iRacing 8.15pm", new string[] { "iracing", "MX 5", "MX-5", "Sun", "Sunday" } },
            { "PCARS2 FR3.5 8pm", new string[] { "FR 35", "FR 3.5", "PC 2", "PCars 2", "Fri", "Friday" } },
            { "RaceRoom Wed 8pm",  new string[] { "R3E", "RaceRoom", "TT", "Wed", "Wednesday" } }
        };

        public static string[] GetAliasesForEventShortname(string eventShortname)
        {
            if (!aliasesDictionary.ContainsKey(eventShortname))
            {
                return null;
            } else
            {
                return aliasesDictionary[eventShortname];
            }
        }
        public static string GetEventShortnameFromAlias(string alias)
        {
            foreach (var aD in aliasesDictionary)
            {
                if (aD.Key == alias)
                {
                    return alias;
                }

                if (aD.Value.Any(a => a.ToLower().Contains(alias.ToLower())) ||
                    aD.Value.Any(a => a.Replace(" ", "").ToLower().Contains(alias.ToLower())))
                    {
                    return aD.Key;
                }
            }

            return null;
        }
    }
}
