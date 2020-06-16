using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Processes;
using Signum.Entities;
using Signum.Engine.Basics;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities.Authorization;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Engine.Operations;
using System.Threading;

namespace Signum.Engine.Processes
{
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
                
                
                QueryLogic.Expressions.Register((PackageEntity p) => p.Lines(), () => ProcessMessage.Lines.NiceToString());

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
                }

            }
        }

        public static void ExceptionLogic_DeletePackages<T>(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token) where T : PackageEntity
        {
            var toDelete = Database.Query<T>().Where(pack => !Database.Query<ProcessEntity>().Any(pr => pr.Data == pack));

            toDelete.SelectMany(a => a.Lines()).UnsafeDeleteChunksLog(parameters, sb, token);
            toDelete.UnsafeDeleteChunksLog(parameters, sb, token);
        }

        public static PackageEntity CreateLines(this PackageEntity package, IEnumerable<Lite<IEntity>> lites)
        {
            package.Save();

            int inserts =
                lites.GroupBy(a => a.EntityType).Sum(gr =>
                    gr.GroupsOf(100).Sum(gr2 =>
                        giInsertPackageLines.GetInvoker(gr.Key)(package, gr2)));

            return package;
        }

        public static PackageEntity CreateLines(this PackageEntity package, IEnumerable<IEntity> entities)
        {
            package.Save();

            int inserts =
                entities.GroupBy(a => a.GetType()).Sum(gr =>
                    gr.GroupsOf(100).Sum(gr2 =>
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

        static readonly GenericInvoker<Func<PackageEntity, IEnumerable<Lite<IEntity>>, int>> giInsertPackageLines = new GenericInvoker<Func<PackageEntity, IEnumerable<Lite<IEntity>>, int>>(
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
                OperationArgs = operationArgs,
            }.CreateLines(entities));
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
    }

    public class PackageOperationAlgorithm : IProcessAlgorithm
    {
        public void Execute(ExecutingProcess executingProcess)
        {
            PackageOperationEntity package = (PackageOperationEntity)executingProcess.Data!;

            OperationSymbol operationSymbol = package.Operation;

            var args = package.OperationArgs;

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

                line.FinishTime = TimeZoneManager.Now;
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

            var args = package.OperationArgs;

            executingProcess.ForEachLine(package.Lines().Where(a => a.FinishTime == null), line =>
            {
                ((T)(IEntity)line.Target).Delete(DeleteSymbol, args);

                line.FinishTime = TimeZoneManager.Now;
                line.Save();
            });
        }
    }

    public class PackageSave<T> : IProcessAlgorithm where T : class, IEntity
    {
        public virtual void Execute(ExecutingProcess executingProcess)
        {
            PackageEntity package = (PackageEntity)executingProcess.Data!;

            var args = package.OperationArgs;

            using (OperationLogic.AllowSave<T>())
                executingProcess.ForEachLine(package.Lines().Where(a => a.FinishTime == null), line =>
                {
                    ((T)(object)line.Target).Save();
                    line.FinishTime = TimeZoneManager.Now;
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

            var args = package.OperationArgs;

            executingProcess.ForEachLine(package.Lines().Where(a => a.FinishTime == null), line =>
            {
                ((T)(object)line.Target).Execute(Symbol, args);
                line.FinishTime = TimeZoneManager.Now;
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

            var args = package.OperationArgs;

            executingProcess.ForEachLine(package.Lines().Where(a => a.FinishTime == null), line =>
            {
                var result = ((F)(object)line.Target).ConstructFrom(Symbol, args);
                if (result.IsNew)
                    result.Save();

                line.Result = ((Entity)(IEntity)result).ToLite();

                line.FinishTime = TimeZoneManager.Now;
                line.Save();
            });
        }
    }
}

