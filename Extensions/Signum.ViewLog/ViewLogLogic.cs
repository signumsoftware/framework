using System.IO;
using Signum.Engine.Sync;
using Signum.Authorization;

namespace Signum.ViewLog;

public static class ViewLogLogic
{
    [AutoExpressionField]
    public static IQueryable<ViewLogEntity> ViewLogs(this Entity a) => 
        As.Expression(() => Database.Query<ViewLogEntity>().Where(log => log.Target.Is(a)));
    
    [AutoExpressionField]
    public static ViewLogEntity? ViewLogMyLast(this Entity e) => As.Expression(() => e.ViewLogs()
        .Where(a => a.User.Is(UserEntity.Current))
        .OrderBy(a => a.StartDate).FirstOrDefault());

    public static Func<Type, bool> LogType = type => true;
    public static Func<BaseQueryRequest, DynamicQueryContainer.ExecuteType, bool> LogQuery = (request, type) => true;
    public static Func<BaseQueryRequest, StringWriter, string> GetQueryData = (request, sw) => request.QueryUrl + "\n\n" + sw.ToString();


    public static bool IsStarted = false;

    public static void Start(SchemaBuilder sb, HashSet<Type> registerExpression)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        IsStarted = true;

        sb.Include<ViewLogEntity>()
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Target,
                e.ViewAction,
                e.User,
                e.Duration,
                e.StartDate,
                e.EndDate,
            });

        ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;

        var exp = Signum.Utilities.ExpressionTrees.Linq.Expr((Entity entity) => entity.ViewLogs());
        var expLast = Signum.Utilities.ExpressionTrees.Linq.Expr((Entity entity) => entity.ViewLogMyLast());

        foreach (var t in registerExpression)
        {
            QueryLogic.Expressions.Register(new ExtensionInfo(t, exp, exp.Body.Type, "ViewLogs", () => typeof(ViewLogEntity).NicePluralName()));
            QueryLogic.Expressions.Register(new ExtensionInfo(t, expLast, expLast.Body.Type, "LastViewLog", () => ViewLogMessage.ViewLogMyLast.NiceToString()));
        }

        QueryLogic.Queries.QueryExecuted += Current_QueryExecuted;
        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += Type_PreDeleteSqlSync;
        ExecutionMode.OnApiRetrieved += ExecutionMode_OnApiRetrieved;
    }

    private static IDisposable? ExecutionMode_OnApiRetrieved(Lite<Entity> entity, string viewAction)
    {
        return ViewLogLogic.LogView(entity, viewAction);
    }

  
    static SqlPreCommand Type_PreDeleteSqlSync(TypeEntity type)
    {
        var t = Schema.Current.Table<ViewLogEntity>();
        var f = ((FieldImplementedByAll)Schema.Current.Field((ViewLogEntity vl) => vl.Target)).TypeColumn;
        return Administrator.DeleteWhereScript(t, f, type.Id);
    }

    static IDisposable? Current_QueryExecuted(DynamicQueryContainer.ExecuteType type, object queryName, BaseQueryRequest? request)
    {
        if (request == null || !LogQuery(request, type) || UserHolder.Current == null)
            return null;

        var old = Connector.CurrentLogger;

        StringWriter sw = new StringWriter();

        Connector.CurrentLogger = old == null ? (TextWriter)sw : new DuplicateTextWriter(sw, old);

        var viewLog = new ViewLogEntity
        {
            Target = QueryLogic.GetQueryEntity(queryName).ToLite(),
            User = UserHolder.Current!.User,
            ViewAction = type.ToString(),
        };

        return new Disposable(() =>
        {
            try
            {
                var str = GetQueryData(request!, sw);

                using (ExecutionContext.SuppressFlow())
                    Task.Factory.StartNew(() =>
                    {
                        using (ExecutionMode.Global())
                        using (var tr = Transaction.ForceNew())
                        {
                            viewLog.EndDate = Clock.Now;
                            viewLog.Data = new BigStringEmbedded(str);
                            using (ExecutionMode.Global())
                                viewLog.Save();
                            tr.Commit();
                        }
                    });
            }
            finally
            {
                Connector.CurrentLogger = old;
            }
        });
    }

    static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
    {
        var dateLimit = parameters.GetDateLimitDelete(typeof(ViewLogEntity).ToTypeEntity());
        if (dateLimit != null)
            Database.Query<ViewLogEntity>().Where(view => view.StartDate < dateLimit.Value).UnsafeDeleteChunksLog(parameters, sb, token);
    }

    public static IDisposable? LogView(Lite<IEntity> entity, string viewAction)
    {
        if (!IsStarted)
            return null;

        if (entity == null || !LogType(entity.EntityType) || UserHolder.Current == null)
            return null;


        var viewLog = new ViewLogEntity
        {
            Target = (Lite<Entity>)entity.Clone(),
            User = UserHolder.Current.User,
            ViewAction = viewAction,
            Data = new BigStringEmbedded(),
        };

        return new Disposable(() =>
        {
            viewLog.EndDate = Clock.Now;
            using (ExecutionMode.Global())
                viewLog.Save();
        });
    }
}

public class DuplicateTextWriter : TextWriter
{
    public TextWriter First;
    public TextWriter Second; 

    public DuplicateTextWriter(TextWriter first, TextWriter second)
    {
        this.First = first;
        this.Second = second;
    }

    public override void Write(char[] buffer, int index, int count)
    {
        First.Write(buffer, index, count);
        Second.Write(buffer, index, count);
    }

    public override void Write(string? value)
    {
        First.Write(value);
        Second.Write(value);
    }

    public override Encoding Encoding
    {
        get { return Encoding.Default; }
    }
}
