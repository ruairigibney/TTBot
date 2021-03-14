using ServiceStack.Data;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTBot.Models;

namespace TTBot.DataAccess
{
    public class ExcelSheetEventMapping : IExcelSheetEventMapping
    {
        private readonly IDbConnectionFactory _conFactory;

        public ExcelSheetEventMapping(IDbConnectionFactory conFactory)
        {
            _conFactory = conFactory;
        }

        public async Task AddAsync(ulong eventId, string sheet, bool isRoundsSheet = false)
        {
            using (var connection = _conFactory.Open())
            {
                await connection.InsertAsync(new ExcelSheetEventMappingModel()
                {
                    EventId = eventId,
                    Sheetname = sheet,
                    IsRoundsSheet = isRoundsSheet
                });
            }
        }

        public async Task RemoveAsync(int id)
        {
            using (var connection = _conFactory.Open())
            {
                var q = connection.From<ExcelSheetEventMappingModel>().Where(x => x.Id == id);

                await connection.DeleteAsync(q);
            }
        }

        public async Task<string> GetEventShortnameFromSheetNameAsync(string sheet, bool isRoundsSheet = false)
        {
            using (var connection = _conFactory.Open())
            {
                var mapping = await connection.SingleAsync<ExcelSheetEventMappingModel>
                    (em => em.Sheetname == sheet && em.IsRoundsSheet == isRoundsSheet);
                if (mapping == null)
                {
                    return null;
                }

                var e = await connection.SingleAsync<Event>(e => e.Id == Convert.ToInt32(mapping.EventId));

                return e.ShortName;
            }
        }

        public async Task<bool> ActiveEventExistsAsync(string worksheet)
        {
            using (var connection = _conFactory.Open())
            {
                var q = connection.From<ExcelSheetEventMappingModel>()
                    .Join<ExcelSheetEventMappingModel, Event>()
                    .Where<Event>(e => e.Closed == false)
                    .Where<ExcelSheetEventMappingModel>(x => x.Sheetname == worksheet);

                var e = await connection.SingleAsync<Event>(q);

                return e != null;
            }
        }

        public async Task<Event> GetActiveEventFromWorksheetAsync(string worksheet)
        {
            using (var connection = _conFactory.Open())
            {
                {
                    var q = connection.From<ExcelSheetEventMappingModel>()
                        .Join<ExcelSheetEventMappingModel, Event>()
                        .Where<Event>(e => e.Closed == false)
                        .Where<ExcelSheetEventMappingModel>(x => x.Sheetname == worksheet);

                    var e = await connection.SingleAsync<Event>(q);
                    return e;
                }
            }
        }

        public async Task<Event> GetEventFromSheetNameAsync(string sheet, bool isRoundsSheet = false)
        {
            using (var connection = _conFactory.Open())
            {
                var mapping = await connection.SingleAsync<ExcelSheetEventMappingModel>
                    (em => em.Sheetname == sheet && em.IsRoundsSheet == isRoundsSheet);
                if (mapping == null)
                {
                    return null;
                }

                var e = await connection.SingleAsync<Event>(e => e.Id == Convert.ToInt32(mapping.EventId));

                return e;
            }
        }

        public async Task<int> GetWorksheetMappingIdAsync(string worksheet)
        {
            using (var connection = _conFactory.Open())
            {
                var q = connection.From<ExcelSheetEventMappingModel>()
                    .Join<ExcelSheetEventMappingModel, Event>()
                    .Where<Event>(e => e.Closed == false)
                    .Where<ExcelSheetEventMappingModel>(x => x.Sheetname == worksheet);

                var w = await connection.SingleAsync(q);
                return w.Id;
            }
        }


        public async Task<List<ExcelSheetEventMappingModel>> GetAllActiveWorksheetMappings()
        {
            using (var connection = _conFactory.Open())
            {
                var q = connection.From<ExcelSheetEventMappingModel>()
                    .OrderBy(x => x.EventId);

                return await connection.SelectAsync(q);
            }
        }
    }
}