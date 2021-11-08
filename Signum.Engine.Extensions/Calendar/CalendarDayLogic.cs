using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Calendar;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Signum.Engine.Calendar
{
    public static class CalendarDayLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodBase.GetCurrentMethod()))
            {
                sb.Include<CalendarDayEntity>()
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Date,
                    });
            }
        }

        public static void CreateDays(DateOnly startDate, DateOnly endDate)
        {
            List<CalendarDayEntity> days = new List<CalendarDayEntity>();
            for (DateOnly d = startDate; d < endDate; d = d.AddDays(1))
                days.Add(new CalendarDayEntity { Date = d });
            days.BulkInsert();
        }
    }
}
