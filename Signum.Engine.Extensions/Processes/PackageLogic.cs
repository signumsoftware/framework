using Signum.Entities.Processes;
using Signum.Utilities.Reflection;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.Json;
using Signum.Entities.Dashboard;

namespace Signum.Engine.Processes;

public static class PackageLogic
{
    [AutoExpressionField]
    public static IQueryable<PackageLineEntity> Lines(this PackageEntity p) => 
        As.Expression(() => Database.Query<PackageLineEntity>().Where(pl => pl.Package.Is(p)));

    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null!, true, true)));
    }

    public static void Start(SchemaBuilder sb, bool packages, bool packageOperations)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            ProcessLogic.AssertStarted(sb);

            sb.Settings.AssertImplementedBy((ProcessExceptionLineEntity pel) => pel.Line, typeof(PackageLineEntity));

            sb.Include<PackageLineEntity>()
                .WithQuery(() => pl => new
                {
                    Entity = pl,
                    pl.Package,
                    pl.Id,
                    pl.Target,
                    pl.Result,
                    pl.FinishTime,
                });

            QueryLogic.Queries.Register(PackageQuery.PackageLineLastProcess, () =>
                from pl in Database.Query<PackageLineEntity>()
                let p = pl.Package.Entity.LastProcess()
                select new
                {
                    Entity = pl,
                    pl.Package,
                    pl.Id,
                    pl.Target,
                    pl.Result,
                    pl.FinishTime,
                    LastProcess = p,
                    Exception = pl.Exception(p),
                });
            
            
            QueryLogic.Expressions.Register((PackageEntity p) => p.Lines(), ProcessMessage.Lines);

            if(packages || packageOperations)
            {
                sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += arg =>
                {
                    var typeEntity = ((TypeEntity)arg);

                    var targetLines = Administrator.UnsafeDeletePreCommand(Database.Query<PackageLineEntity>().Where(pl => pl.Target.GetType().ToTypeEntity().Is(typeEntity)));
                    var resultLines = Administrator.UnsafeDeletePreCommand(Database.Query<PackageLineEntity>().Where(pl => pl.Result!.Entity.GetType().ToTypeEntity().Is(typeEntity)));
                    return SqlPreCommand.Combine(Spacing.Simple, targetLines, resultLines);
                };
            }

            if (packages)
            {
                sb.Settings.AssertImplementedBy((PackageLineEntity pl) => pl.Package, typeof(PackageEntity));
                sb.Settings.AssertImplementedBy((ProcessEntity pe) => pe.Data, typeof(PackageEntity));

                sb.Include<PackageEntity>()
                    .WithQuery(() => pk => new
                    {
                        Entity = pk,
                        pk.Id,
                        pk.Name,
                    });

                QueryLogic.Queries.Register(PackageQuery.PackageLastProcess, () =>
                    from pk in Database.Query<PackageEntity>()
                    let pe = pk.LastProcess()
                    select new
                    {
                        Entity = pk,
                        pk.Id,
                        pk.Name,
                        NumLines = pk.Lines().Count(),
                        LastProcess = pe,
                        NumErrors = pk.Lines().Count(l => l.Exception(pe) != null),
                    });

                ExceptionLogic.DeleteLogs += ExceptionLogic_DeletePackages<PackageEntity>;
            }

            if (packageOperations)
            {
                sb.Settings.AssertImplementedBy((PackageLineEntity pl) => pl.Package, typeof(PackageOperationEntity));
                sb.Settings.AssertImplementedBy((ProcessEntity pe) => pe.Data, typeof(PackageOperationEntity));

                sb.Include<PackageOperationEntity>()
                      .WithQuery(() => pk => new
                      {
                          Entity = pk,
                          pk.Id,
                          pk.Name,
                          pk.Operation,
                      });

                QueryLogic.Queries.Register(PackageQuery.PackageOperationLastProcess, () =>
                    from p in Database.Query<PackageOperationEntity>()
                    let pe = p.LastProcess()
                    select new
                    {
                        Entity = p,
                        p.Id,
                        p.Name,
                        p.Operation,
                        NumLines = p.Lines().Count(),
                        LastProcess = pe,
                        NumErrors = p.Lines().Count(l => l.Exception(pe) != null),
                    });

                ProcessLogic.Register(PackageOperationProcess.PackageOperation, new PackageOperationAlgorithm());
                ExceptionLogic.DeleteLogs += ExceptionLogic_DeletePackages<PackageOperationEntity>;

                sb.Schema.Table<OperationSymbol>().PreDeleteSqlSync += arg =>
                {
                    var operation = (OperationSymbol)arg;

                    if (!Administrator.ExistsTable<PackageOperationEntity>() || !Database.Query<PackageOperationEntity>().Any(a => a.Operation.Is(operation)))
                        return null;

                    var processExceptionLine = Administrator.UnsafeDeletePreCommand(Database.Query<ProcessExceptionLineEntity>().Where(pel => ((PackageOperationEntity)pel.Process.Entity.Data!).Operation.Is(operation)));
                    var process = Administrator.UnsafeDeletePreCommand(Database.Query<ProcessEntity>().Where(p => ((PackageOperationEntity)p.Data!).Operation.Is(operation)));
                    var packageLines = Administrator.UnsafeDeletePreCommand(Database.Query<PackageLineEntity>().Where(pl => ((PackageOperationEntity)pl.Package.Entity).Operation.Is(operation)));
                    var packageOperation = Administrator.UnsafeDeletePreCommand(Database.Query<PackageOperationEntity>().Where(po => po.Operation.Is(operation)));
                    return SqlPreCommand.Combine(Spacing.Simple, processExceptionLine, process, packageLines, packageOperation);
                };
            }

        }
    }

    public static void ExceptionLogic_DeletePackages<T>(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token) where T : PackageEntity
    {
        var query = Database.Query<T>().Where(pack => !Database.Query<ProcessEntity>().Any(pr => pr.Data == pack));

        query.SelectMany(a => a.Lines()).UnsafeDeleteChunksLog(parameters, sb, token);
        query.Where(a => !a.Lines().Any()).UnsafeDeleteChunksLog(parameters, sb, token);
    }

    public static PackageEntity CreateLines(this PackageEntity package, IEnumerable<Lite<IEntity>> lites)
    {
        package.Save();

        int inserts =
            lites.GroupBy(a => a.EntityType).Sum(gr =>
                gr.Chunk(100).Sum(gr2 =>
                    giInsertPackageLines.GetInvoker(gr.Key)(package, gr2)));

        return package;
    }

    public static PackageEntity CreateLines(this PackageEntity package, IEnumerable<IEntity> entities)
    {
        package.Save();

        int inserts =
            entities.GroupBy(a => a.GetType()).Sum(gr =>
                gr.Chunk(100).Sum(gr2 =>
                    giInsertPackageLines.GetInvoker(gr.Key)(package, gr2.Select(a => a.ToLite()))));

        return package;
    }

    public static PackageEntity CreateLinesQuery<T>(this PackageEntity package, IQueryable<T> entities) where T : Entity
    {
        package.Save();

        entities.UnsafeInsert(e => new PackageLineEntity
        {
            Package = package.ToLite(),
            Target = e,
        }); 

        return package;
    }

    static readonly GenericInvoker<Func<PackageEntity, IEnumerable<Lite<IEntity>>, int>> giInsertPackageLines = new(
        (package, lites) => InsertPackageLines<Entity>(package, lites));
    static int InsertPackageLines<T>(PackageEntity package, IEnumerable<Lite<IEntity>> lites)
        where T :Entity
    {
        return Database.Query<T>().Where(p => lites.Contains(p.ToLite())).UnsafeInsert(p => new PackageLineEntity
        {
            Package = package.ToLite(),
            Target = p,
        }); 
    }

    public static ProcessEntity CreatePackageOperation(IEnumerable<Lite<IEntity>> entities, OperationSymbol operation, params object?[]? operationArgs)
    {
        return ProcessLogic.Create(PackageOperationProcess.PackageOperation, new PackageOperationEntity()
        {
            Operation = operation,
        }.SetOperationArgs(operationArgs).CreateLines(entities));
    }

    public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
    {
        TypeConditionLogic.RegisterCompile<ProcessEntity>(typeCondition,
            pe => pe.User.Is(UserEntity.Current));

        TypeConditionLogic.Register<PackageOperationEntity>(typeCondition,
            po => Database.Query<ProcessEntity>().WhereCondition(typeCondition).Any(pe => pe.Data == po));

        TypeConditionLogic.Register<PackageLineEntity>(typeCondition,
            pl => ((PackageOperationEntity)pl.Package.Entity).InCondition(typeCondition));
    }

    public static object?[]? GetOperationArgs(this PackageEntity package)
    {
        return package.OperationArguments == null ? null : 
            (object?[])JsonExtensions.FromJsonBytes<object[]>(package.OperationArguments, EntityJsonContext.FullJsonSerializerOptions);
    }

    public static PackageEntity SetOperationArgs(this PackageEntity package, object?[]? args)
    {
        package.OperationArguments = args == null ? null : JsonExtensions.ToJsonBytes(args, EntityJsonContext.FullJsonSerializerOptions);
        return package;
    }
}

public class PackageOperationAlgorithm : IProcessAlgorithm
{
    public void Execute(ExecutingProcess executingProcess)
    {
        PackageOperationEntity package = (PackageOperationEntity)executingProcess.Data!;

        OperationSymbol operationSymbol = package.Operation;

        var args = package.GetOperationArgs();

        executingProcess.ForEachLine(package.Lines().Where(a => a.FinishTime == null), line =>
        {
            OperationType operationType = OperationLogic.OperationType(line.Target.GetType(), operationSymbol);

            switch (operationType)
            {
                case OperationType.Execute:
                    OperationLogic.ServiceExecute(line.Target, operationSymbol, args);
                    break;
                case OperationType.Delete:
                    OperationLogic.ServiceDelete(line.Target, operationSymbol, args);
                    break;
                case OperationType.ConstructorFrom:
                    {
                        var result = OperationLogic.ServiceConstructFrom(line.Target, operationSymbol, args);
                        line.Result = result.ToLite();
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unexpected operation type {0}".FormatWith(operationType));
            }

            line.FinishTime = Clock.Now;
            line.Save();
        });
    }
}

public class PackageDeleteAlgorithm<T> : IProcessAlgorithm where T : class, IEntity
{
    public DeleteSymbol<T> DeleteSymbol { get; private set; }
    
    public PackageDeleteAlgorithm(DeleteSymbol<T> deleteSymbol)
    {
        this.DeleteSymbol = deleteSymbol ?? throw new ArgumentNullException("operatonKey");
    }

    public virtual void Execute(ExecutingProcess executingProcess)
    {
        PackageEntity package = (PackageEntity)executingProcess.Data!;

        var args = package.GetOperationArgs();

        executingProcess.ForEachLine(package.Lines().Where(a => a.FinishTime == null), line =>
        {
            ((T)(IEntity)line.Target).Delete(DeleteSymbol, args);

            line.FinishTime = Clock.Now;
            line.Save();
        });
    }
}
   
public class PackageExecuteAlgorithm<T> : IProcessAlgorithm where T : class, IEntity
{
    public ExecuteSymbol<T> Symbol { get; private set; }

    public PackageExecuteAlgorithm(ExecuteSymbol<T> symbol)
    {
        this.Symbol = symbol ?? throw new ArgumentNullException("operationKey");
    }

    public virtual void Execute(ExecutingProcess executingProcess)
    {
        PackageEntity package = (PackageEntity)executingProcess.Data!;

        var args = package.GetOperationArgs();

        executingProcess.ForEachLine(package.Lines().Where(a => a.FinishTime == null), line =>
        {
            ((T)(object)line.Target).Execute(Symbol, args);
            line.FinishTime = Clock.Now;
            line.Save();
        });
    }
}

public class PackageConstructFromAlgorithm<F, T> : IProcessAlgorithm
    where T : class, IEntity
    where F : class, IEntity
{
    public ConstructSymbol<T>.From<F> Symbol { get; private set; }

    public PackageConstructFromAlgorithm(ConstructSymbol<T>.From<F> symbol)
    {
        this.Symbol = symbol;
    }

    public virtual void Execute(ExecutingProcess executingProcess)
    {
        PackageEntity package = (PackageEntity)executingProcess.Data!;

        var args = package.GetOperationArgs();

        executingProcess.ForEachLine(package.Lines().Where(a => a.FinishTime == null), line =>
        {
            var result = ((F)(object)line.Target).ConstructFrom(Symbol, args);
            if (result != null)
            {
                if (result.IsNew)
                {
                    using (OperationLogic.AllowSave<T>())
                        result.Save();
                }

                line.Result = ((Entity)(IEntity)result).ToLite();
            }

            line.FinishTime = Clock.Now;
            line.Save();
        });
    }
}

