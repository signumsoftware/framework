using Signum.Engine.Sync;

namespace Signum.Scheduler;

public static class SimpleTaskLogic
{
    static Dictionary<SimpleTaskSymbol, Func<ScheduledTaskContext, Lite<IEntity>?>> tasks = new Dictionary<SimpleTaskSymbol, Func<ScheduledTaskContext, Lite<IEntity>?>>();

    internal static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        SymbolLogic<SimpleTaskSymbol>.Start(sb, () => tasks.Keys.ToHashSet());

        SchedulerLogic.ExecuteTask.Register((SimpleTaskSymbol st, ScheduledTaskContext ctx) =>
        {
            Func<ScheduledTaskContext, Lite<IEntity>?> func = tasks.GetOrThrow(st);
            return func(ctx);
        });

        sb.Include<SimpleTaskSymbol>()
            .WithQuery(() => ct => new
            {
                Entity = ct,
                ct.Id,
                ct.Key,
            });

        sb.Schema.EntityEvents<SimpleTaskSymbol>().PreDeleteSqlSync += SimpleTaskLogic_PreDeleteSqlSync;

    }

    private static SqlPreCommand? SimpleTaskLogic_PreDeleteSqlSync(SimpleTaskSymbol st)
    {
        var deleteLogs =Administrator.DeleteWhereScript((ScheduledTaskLogEntity ol) => ol.Task, st);
        var deleteTasks = Administrator.DeleteWhereScript((ScheduledTaskEntity ol) => ol.Task, st);

        return SqlPreCommand.Combine(Spacing.Double, deleteLogs, deleteTasks);
    }

    public static void Register(SimpleTaskSymbol simpleTaskSymbol, Func<ScheduledTaskContext, Lite<IEntity>?> action)
    {
        if (simpleTaskSymbol == null)
            throw AutoInitAttribute.ArgumentNullException(typeof(SimpleTaskSymbol), nameof(simpleTaskSymbol));

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        tasks.Add(simpleTaskSymbol, action); 
    }      
}
