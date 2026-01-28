
namespace Signum.Calendar;

public static class CalendarDayLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<CalendarDayEntity>()
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Date,
            });
    }

    public static void CreateDays(DateOnly startDate, DateOnly endDate)
    {
        List<CalendarDayEntity> days = new List<CalendarDayEntity>();
        for (DateOnly d = startDate; d < endDate; d = d.AddDays(1))
            days.Add(new CalendarDayEntity { Date = d });
        days.BulkInsert();
    }
}
