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
using Signum.Entities.Scheduler;
using Signum.Utilities.Reflection;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Engine.Exceptions;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Engine.Operations;

namespace Signum.Engine.Processes
{
    public static class PackageLogic
    {
        static Expression<Func<PackageDN, IQueryable<PackageLineDN>>> LinesExpression =
            p => Database.Query<PackageLineDN>().Where(pl => pl.Package.RefersTo(p));
        public static IQueryable<PackageLineDN> Lines(this PackageDN p)
        {
            return LinesExpression.Evaluate(p);
        }

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null, null, true, true)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool packages, bool packageOperations)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                ProcessLogic.AssertStarted(sb);

                sb.Settings.AssertImplementedBy((ProcessExceptionLineDN pel) => pel.Line, typeof(PackageLineDN));

                sb.Include<PackageLineDN>();
                dqm.RegisterQuery(typeof(PackageLineDN), () =>
                    from pl in Database.Query<PackageLineDN>()
                    let p = pl.Package.Entity.LastProcess()
                    select new
                    {
                        Entity = pl,
                        pl.Package,
                        pl.Id,
                        pl.Target,
                        pl.FinishTime,
                        LastProcess = p,
                        Exception = pl.Exception(p)
                    });


                dqm.RegisterExpression((PackageDN p) => p.Lines(), () => ProcessMessage.Lines.NiceToString());

                if (packages)
                {
                    sb.Settings.AssertImplementedBy((PackageLineDN pl) => pl.Package, typeof(PackageDN));
                    sb.Settings.AssertImplementedBy((ProcessDN pe) => pe.Data, typeof(PackageDN));

                    sb.Include<PackageDN>();

                    dqm.RegisterQuery(typeof(PackageDN), () =>
                        from pk in Database.Query<PackageDN>()
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
                }

                if (packageOperations)
                {
                    sb.Settings.AssertImplementedBy((PackageLineDN pl) => pl.Package, typeof(PackageOperationDN));
                    sb.Settings.AssertImplementedBy((ProcessDN pe) => pe.Data, typeof(PackageOperationDN));

                    sb.Include<PackageOperationDN>();

                    dqm.RegisterQuery(typeof(PackageOperationDN), () =>
                        from p in Database.Query<PackageOperationDN>()
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
                }

                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;
            }
        }

        public static void ExceptionLogic_DeleteLogs(DateTime limit)
        {
            var usedDatas = Database.Query<ProcessDN>().Select(a => a.Data);

            Database.Query<PackageLineDN>().Where(line => !usedDatas.Contains(line.Package.Entity)).UnsafeDelete();
            Database.Query<PackageOperationDN>().Where(po => !usedDatas.Contains(po)).UnsafeDelete();
            Database.Query<PackageDN>().Where(po => !usedDatas.Contains(po)).UnsafeDelete();
        }

        public static PackageDN CreateLines(this PackageDN package, IEnumerable<Lite<IIdentifiable>> lites)
        {
            package.Save();

            int inserts =
                lites.GroupBy(a => a.EntityType).Sum(gr =>
                    gr.GroupsOf(100).Sum(gr2 =>
                        giInsertPackageLines.GetInvoker(gr.Key)(package, gr2)));

            return package;
        }

        public static PackageDN CreateLines(this PackageDN package, IEnumerable<IIdentifiable> entities)
        {
            package.Save();

            int inserts =
                entities.GroupBy(a => a.GetType()).Sum(gr =>
                    gr.GroupsOf(100).Sum(gr2 =>
                        giInsertPackageLines.GetInvoker(gr.Key)(package, gr2.Select(a => a.ToLite()))));

            return package;
        }

        public static PackageDN CreateLinesQuery<T>(this PackageDN package, IQueryable<T> entities) where T : IdentifiableEntity
        {
            package.Save();

            entities.UnsafeInsert(e => new PackageLineDN
            {
                Package = package.ToLite(),
                Target = e,
            }); 

            return package;
        }

        static readonly GenericInvoker<Func<PackageDN, IEnumerable<Lite<IIdentifiable>>, int>> giInsertPackageLines = new GenericInvoker<Func<PackageDN, IEnumerable<Lite<IIdentifiable>>, int>>(
            (package, lites) => InsertPackageLines<Entity>(package, lites));
        static int InsertPackageLines<T>(PackageDN package, IEnumerable<Lite<IIdentifiable>> lites)
            where T :IdentifiableEntity
        {
            return Database.Query<T>().Where(p => lites.Contains(p.ToLite())).UnsafeInsert(p => new PackageLineDN
            {
                Package = package.ToLite(),
                Target = p,
            }); 
        }

        public static ProcessDN CreatePackageOperation(IEnumerable<Lite<IIdentifiable>> entities, OperationSymbol operation, params object[] operationArgs)
        {
            return ProcessLogic.Create(PackageOperationProcess.PackageOperation, new PackageOperationDN()
            {
                Operation = operation,
                OperationArgs = operationArgs,
            }.CreateLines(entities));
        }

        public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            TypeConditionLogic.RegisterCompile<ProcessDN>(typeCondition,
                pe => pe.Mixin<UserProcessSessionMixin>().User.RefersTo(UserDN.Current));

            TypeConditionLogic.Register<PackageOperationDN>(typeCondition,
                po => Database.Query<ProcessDN>().WhereCondition(typeCondition).Any(pe => pe.Data == po));

            TypeConditionLogic.Register<PackageLineDN>(typeCondition,
                pl => ((PackageOperationDN)pl.Package.Entity).InCondition(typeCondition));
        }
    }

    public class PackageOperationAlgorithm : IProcessAlgorithm
    {
        public void Execute(ExecutingProcess executingProcess)
        {
            PackageOperationDN package = (PackageOperationDN)executingProcess.Data;

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
                        throw new InvalidOperationException("Unexpected operation type {0}".Formato(operationType));
                }

                line.FinishTime = TimeZoneManager.Now;
                line.Save();
            });
        }
    }

    public class PackageDeleteAlgorithm<T> : IProcessAlgorithm where T : class, IIdentifiable
    {
        public DeleteSymbol<T> DeleteSymbol { get; private set; }

        public Func<PackageDN, PackageLineDN, object[]> OperationArgs;

        public PackageDeleteAlgorithm(DeleteSymbol<T> deleteSymbol)
        {
            if (deleteSymbol == null)
                throw new ArgumentNullException("operatonKey");

            this.DeleteSymbol = deleteSymbol;
        }

        public virtual void Execute(ExecutingProcess executingProcess)
        {
            PackageDN package = (PackageDN)executingProcess.Data;

            var args = package.OperationArgs;

            executingProcess.ForEachLine(package.Lines().Where(a => a.FinishTime == null), line =>
            {
                ((T)(IIdentifiable)line.Target).Delete<T, T>(DeleteSymbol, args);

                line.FinishTime = TimeZoneManager.Now;
                line.Save();
            });
        }
    }
   
    public class PackageExecuteAlgorithm<T> : IProcessAlgorithm where T : class, IIdentifiable
    {
        public ExecuteSymbol<T> Symbol { get; private set; }

        public PackageExecuteAlgorithm(ExecuteSymbol<T> symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException("operationKey");

            this.Symbol = symbol;
        }

        public virtual void Execute(ExecutingProcess executingProcess)
        {
            PackageDN package = (PackageDN)executingProcess.Data;

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
        where T : class, IIdentifiable
        where F : class, IIdentifiable
    {
        public ConstructSymbol<T>.From<F> Symbol { get; private set; }
        public Enum OperationKey { get; private set; }

        public PackageConstructFromAlgorithm(ConstructSymbol<T>.From<F> symbol)
        {
            this.Symbol = symbol;
        }

        public virtual void Execute(ExecutingProcess executingProcess)
        {
            PackageDN package = (PackageDN)executingProcess.Data;

            var args = package.OperationArgs;

            executingProcess.ForEachLine(package.Lines(), line =>
            {
                var result = ((F)(object)line.Target).ConstructFrom(Symbol, args);
                if (result.IsNew)
                    result.Save();

                line.Result = ((IdentifiableEntity)(IIdentifiable)result).ToLite();

                line.FinishTime = TimeZoneManager.Now;
                line.Save();
            });
        }
    }
}

