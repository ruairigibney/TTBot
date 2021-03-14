using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace TTBot.Models
{

    [Alias("ExcelSheetEventMapping")]
    public class ExcelSheetEventMappingModel
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }
        [ForeignKey(typeof(Event))]
        public ulong EventId { get; set; }
        public string Sheetname { get; set; }
        public bool IsRoundsSheet { get; set; }
    }
}
