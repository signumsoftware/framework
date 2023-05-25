
namespace Signum.Calendar;

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class CalendarDayEntity : Entity
{
    public DateOnly Date { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Date.ToShortDateString());
}
