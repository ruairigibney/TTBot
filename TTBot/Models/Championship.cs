using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace TTBot.Models
{
    [Alias("Championship")]
    public class Championship
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

    }
}
