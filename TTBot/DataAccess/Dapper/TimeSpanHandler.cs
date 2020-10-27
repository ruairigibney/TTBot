using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace TTBot.DataAccess.Dapper
{
    public class TimeSpanHandler : SqlMapper.TypeHandler<TimeSpan>
    {
        public override TimeSpan Parse(object value)
        {
            return TimeSpan.FromTicks((long)value);
        }

        public override void SetValue(IDbDataParameter parameter, TimeSpan value)
        {
            parameter.Value = value.Ticks;
        }
    }
}
