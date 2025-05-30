using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using Signum.Engine.Sync;
using Signum.Entities;

namespace Signum.Basics;

public static class ExceptionLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            sb.Include<ExceptionEntity>()
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.CreationDate,
                    e.ExceptionType,
                    e.Origin,
                    e.User,
                    e.ExceptionMessage,
                    e.StackTraceHash,
                });

            DefaultEnvironment = "Default";
        }
    }

    public static event Action<Exception, ExceptionEntity?>? OnExceptionLogged;

    public static ExceptionEntity LogException(this Exception ex, Action<ExceptionEntity>? completeContext = null)
    {
        ExceptionEntity? entity = null;
        try
        {
            entity = GetEntity(ex);
            completeContext?.Invoke(entity);
            entity.SaveForceNew();

            return entity;
        }
        finally
        {
            if (OnExceptionLogged is not null)
                OnExceptionLogged(ex, entity);
        }
    }

    public static ExceptionEntity? GetExceptionEntity(this Exception ex)
    {
        var exEntity = ex.Data[ExceptionEntity.ExceptionDataKey] as ExceptionEntity;

        return exEntity;
    }

    static ExceptionEntity GetEntity(Exception ex)
    {
        ExceptionEntity entity = ex.GetExceptionEntity() ?? new ExceptionEntity(ex);

        entity.ExceptionType = ex.GetType().Name;

        var exceptions = ex is AggregateException agex ?
            agex.InnerExceptions.SelectMany(inner => inner.Follow(e => e.InnerException)).ToList() :
            ex.Follow(e => e.InnerException).ToList();

        string messages = exceptions.ToString(e => e.Message, "\n");
        string stacktraces = exceptions.ToString(e => e.StackTrace, "\n");

        entity.ExceptionMessage = messages.DefaultText("- No message - ");
        entity.StackTrace = new BigStringEmbedded(stacktraces.DefaultText("- No stacktrace -"));
        entity.ThreadId = Thread.CurrentThread.ManagedThreadId;
        entity.ApplicationName = Schema.Current.ApplicationName;
        entity.HResult = ex.HResult;
        entity.Form = new BigStringEmbedded();
        entity.QueryString = new BigStringEmbedded();
        entity.Session = new BigStringEmbedded();

        entity.Environment = CurrentEnvironment;
        try
        {
            entity.User = UserHolder.Current?.User; //Session special situations
        }
        catch { }

        try
        {
            entity.Data = new BigStringEmbedded(ObjectDumper.Dump(ex.Data));
        }
        catch (Exception e)
        {
            entity.Data = new BigStringEmbedded($@"Error Dumping Data!{e.GetType().Name}: {e.Message}{e.StackTrace}");
        }

        entity.Version = Schema.Current.Version.ToString();

        return entity;
    }

    static ExceptionEntity SaveForceNew(this ExceptionEntity entity)
    {
        if (entity.Modified == ModifiedState.Clean)
            return entity;

        using (ExecutionMode.Global())
        using (var tr = Transaction.ForceNew())
        {
            entity.Save();

            return tr.Commit(entity);
        }
    }

    public static string? DefaultEnvironment { get; set; }

    public static string? CurrentEnvironment { get { return overridenEnvironment.Value ?? DefaultEnvironment; } }

    static readonly Variable<string?> overridenEnvironment = Statics.ThreadVariable<string?>("exceptionEnviroment");

    public static IDisposable OverrideEnviroment(string? newEnviroment)
    {
        string? oldEnviroment = overridenEnvironment.Value;
        overridenEnvironment.Value = newEnviroment;
        return new Disposable(() => overridenEnvironment.Value = oldEnviroment);
    }


    public static event Action<DeleteLogParametersEmbedded, StringBuilder, CancellationToken>? DeleteLogs;

    public static int DeleteLogsTimeOut = 10 * 60 * 1000;

    public static void DeleteLogsAndExceptions(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
    {
        using (Connector.CommandTimeoutScope(DeleteLogsTimeOut))
        using (var tr = Transaction.None())
        {
            foreach (var action in DeleteLogs.GetInvocationListTyped())
            {
                token.ThrowIfCancellationRequested();

                action(parameters, sb, token);
            }

            WriteRows(sb, "Updating ExceptionEntity.Referenced = false", () => Database.Query<ExceptionEntity>().UnsafeUpdate().Set(a => a.Referenced, a => false).Execute());

            token.ThrowIfCancellationRequested();

            var ex = Schema.Current.Table<ExceptionEntity>();
            var referenced = (FieldValue)ex.GetField(ReflectionTools.GetPropertyInfo((ExceptionEntity e) => e.Referenced));

            var commands = (from t in Schema.Current.GetDatabaseTables()
                            from c in t.Columns.Values
                            where c.ReferenceTable == ex
                            select (table: t, command: new SqlPreCommandSimple("UPDATE ex SET {1} = 1 FROM {0} ex JOIN {2} log ON ex.Id = log.{3}"
                               .FormatWith(ex.Name, referenced.Name, t.Name, c.Name)))).ToList();

            foreach (var (table, command) in commands)
            {
                token.ThrowIfCancellationRequested();

                WriteRows(sb, "Updating Exceptions.Referenced from " + table.Name.Name, () => command.ExecuteNonQuery());
            }

            token.ThrowIfCancellationRequested();

            var dateLimit = parameters.GetDateLimitDelete(typeof(ExceptionEntity).ToTypeEntity());

            if (dateLimit != null)
            {
                Database.Query<ExceptionEntity>()
                    .Where(a => !a.Referenced && a.CreationDate < dateLimit)
                    .UnsafeDeleteChunksLog(parameters, sb, token);
            }

            tr.Commit();
        }
    }

    public static void WriteRows(StringBuilder sb, string text, Func<int> makeQuery)
    {
        var start = PerfCounter.Ticks;

        var result = makeQuery();

        var end = PerfCounter.Ticks;

        var ts = TimeSpan.FromMilliseconds(PerfCounter.ToMilliseconds(start, end));

        sb.AppendLine($"{text}: {result} rows affected in {ts.NiceToString()}");
    }

    public static void UnsafeDeleteChunksLog<T>(this IQueryable<T> sources, DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken cancellationToken)
        where T : Entity
    {
        WriteRows(sb, "Deleting " + typeof(T).Name, () => sources.UnsafeDeleteChunks(
            parameters.ChunkSize,
            parameters.MaxChunks,
            pauseMilliseconds: parameters.PauseTime,
            cancellationToken: cancellationToken));
    }

    public static void ExecuteChunksLog<T>(this IUpdateable<T> sources, DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken cancellationToken)
      where T : Entity
    {
        WriteRows(sb, "Updating " + typeof(T).Name, () => sources.ExecuteChunks(
            parameters.ChunkSize,
            parameters.MaxChunks,
            pauseMilliseconds: parameters.PauseTime,
            cancellationToken: cancellationToken));
    }
}
