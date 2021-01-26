using System;
using System.Collections.Generic;
using System.Text;

namespace TTBot.Models
{
    class ExcelSheetShortnameMappingModel
    {
        public static Dictionary<string, string> mappings = new Dictionary<string, string>()
        {
            {"ACC ProAmChampionship", "ACC GT3 8pm"},
            {"ACC Beg Championship","ACC GT3 7.30pm"},
            {"iRacing MX5 Championship","iRacing 8.15pm"},
            {"FR3.5 Championship", "PCARS2 FR3.5 8pm"},
            {"AudiTT Championship","RaceRoom Wed 8pm"}
        };
    }
}
