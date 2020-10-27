using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static Dapper.SqlMapper;

namespace TTBot.DataAccess.Dapper
{
    public class UlongTypeHandler : TypeHandler<UInt64>
    {
        public override ulong Parse(object value)
        {
            return UInt64.Parse(value as string);
        }

        public override void SetValue(IDbDataParameter parameter, UInt64 value)
        {
            parameter.DbType = DbType.UInt64;
            parameter.Value = value.ToString();
        }
    }
}
