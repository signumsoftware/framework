using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Processes;
using Signum.Entities;
using Signum.Engine.Operations;
using Signum.Entities.Operations;
using Signum.Engine.Basics;
using System.Reflection;
using Signum.Entities.Scheduler;
using Signum.Utilities.Reflection;
using Signum.Engine.Extensions.Properties;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Entities.Exceptions;
using Signum.Engine.Exceptions;
using System.Linq.Expressions;
using Signum.Utilities;

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

        static Expression<Func<PackageDN, IQueryable<PackageLineDN>>> RemainingLinesExpression =
            p => p.Lines().Where(a => a.FinishTime == null && a.Exception == null);
        public static IQueryable<PackageLineDN> RemainingLines(this PackageDN p)
        {
            return RemainingLinesExpression.Evaluate(p);
        }

        static Expression<Func<PackageDN, IQueryable<PackageLineDN>>> ExceptionLinesExpression =
            p => p.Lines().Where(a => a.Exception != null);
        public static IQueryable<PackageLineDN> ExceptionLines(this PackageDN p)
        {
            return ExceptionLinesExpression.Evaluate(p);
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

                sb.Include<PackageLineDN>();

                if (packages)
                {
                    sb.Settings.AssertImplementedBy((PackageLineDN pl) => pl.Package, typeof(PackageDN));
                    sb.Settings.AssertImplementedBy((ProcessExecutionDN pe) => pe.ProcessData, typeof(PackageDN));

                    sb.Include<PackageDN>();
                    dqm[typeof(PackageDN)] =
                        (from p in Database.Query<PackageDN>()
                         select new
                         {
                             Entity = p,
                             p.Id,
                             p.Name,
                             Lines = p.Lines().Count()
                         }).ToDynamic();

                }

                if (packageOperations)
                {
                    sb.Settings.AssertImplementedBy((PackageLineDN pl) => pl.Package, typeof(PackageOperationDN));
                    sb.Settings.AssertImplementedBy((ProcessExecutionDN pe) => pe.ProcessData, typeof(PackageOperationDN));


                    sb.Include<PackageOperationDN>();
                    dqm[typeof(PackageOperationDN)] =
                        (from p in Database.Query<PackageOperationDN>()
                         select new
                         {
                             Entity = p,
                             p.Id,
                             p.Name,
                             p.Operation,
                             Lines = p.Lines().Count()
                         }).ToDynamic();

                    ProcessLogic.Register(PackageOperationProcess.ExecutePackageOperation, new PackageOperationAlgorithm());

                    new BasicConstructFromMany<IdentifiableEntity, ProcessExecutionDN>(PackageOperationOperation.CreatePackageOperation)
                    {
                        Construct = (entities, args) =>
                        {
                            if (args == null || args.Length < 0 || !(args[0] is OperationDN || args[0] is Enum))
                                throw new ArgumentNullException("{0} requires an operation as the first argument".Formato(PackageOperationOperation.CreatePackageOperation));

                            OperationDN operation = args[0] as OperationDN ?? MultiEnumLogic<OperationDN>.ToEntity((Enum)args[0]);

                            var process = ProcessLogic.Create(PackageOperationProcess.ExecutePackageOperation, new PackageOperationDN()
                            {
                                Operation = operation
                            }.CreateLines(entities));

                            return process;
                        }
                    }.Register();
                }

                dqm[typeof(PackageLineDN)] =
                    (from pl in Database.Query<PackageLineDN>()
                     select new
                     {
                         Entity = pl,
                         Package = pl.Package,
                         pl.Id,
                         Target = pl.Entity,
                         pl.FinishTime,
                         pl.Exception,
                     }).ToDynamic();

                dqm.RegisterExpression((PackageDN p) => p.Lines());
                dqm.RegisterExpression((PackageDN p) => p.RemainingLines());
                dqm.RegisterExpression((PackageDN p) => p.ExceptionLines());
            }
        }

        public static PackageDN CreateLines(this PackageDN package, IEnumerable<Lite> lites)
        {
            package.Save();

            lites.Select(lite => new PackageLineDN
            {
                Package = package.ToLite(),
                Entity = lite.ToLite<IIdentifiable>()
            }).SaveList();

            return package;
        }

        public static void ForEachLine(this IExecutingProcess executingProcess, PackageDN package, Action<PackageLineDN> action)
        {
            List<PackageLineDN> lines = package.RemainingLines().ToList();

            for (int i = 0; i < lines.Count; i++)
            {
                executingProcess.CancellationToken.ThrowIfCancellationRequested();

                PackageLineDN pl = lines[i];

                try
                {
                    using (Transaction tr = Transaction.ForceNew())
                    {
                        action(pl);
                        pl.FinishTime = TimeZoneManager.Now;
                        pl.Save();
                        tr.Commit();
                    }
                }
                catch (Exception e)
                {
                    if (Transaction.InTestTransaction)
                        throw;

                    var exLog = e.LogException();

                    using (Transaction tr = Transaction.ForceNew())
                    {
                        pl.Exception = exLog.ToLite();
                        pl.Save();
                        tr.Commit();
                    }
                }

                executingProcess.ProgressChanged(i, lines.Count);
            }
        }
    }

    public class PackageOperationAlgorithm : IProcessAlgorithm
    {
        public void Execute(IExecutingProcess executingProcess)
        {
            PackageOperationDN package = (PackageOperationDN)executingProcess.Data;

            Enum operationKey = MultiEnumLogic<OperationDN>.ToEnum(package.Operation);

            executingProcess.ForEachLine(package, line =>
            {
                OperationType operationType = OperationLogic.OperationType(line.Entity.RuntimeType, operationKey);

                switch (operationType)
                {
                    case OperationType.Execute:
                        OperationLogic.ServiceExecuteLite(line.Entity, operationKey);
                        break;
                    case OperationType.Delete:
                        OperationLogic.ServiceDelete(line.Entity, operationKey);
                        break;
                    case OperationType.ConstructorFrom:
                        {
                            var result = OperationLogic.ServiceExecuteLite(line.Entity, operationKey);
                            if (result.IsNew)
                                result.Save();

                            line.Result = result.ToLite<IIdentifiable>();
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected operation type {0}".Formato(operationType));
                }
            });
        }
    }

   
    public class PackageExecuteAlgorithm<T> : IProcessAlgorithm where T : class, IIdentifiable
    {
        public Enum OperationKey { get; private set; }

        public PackageExecuteAlgorithm(Enum operationKey)
        {
            if(operationKey == null)
                throw new ArgumentNullException("operatonKey");

            this.OperationKey = operationKey;
        }

        public virtual void Execute(IExecutingProcess executingProcess)
        {
            PackageDN package = (PackageDN)executingProcess.Data;

            executingProcess.ForEachLine(package, line =>
            {
                line.Entity.ToLite<T>().ExecuteLite<T>(OperationKey);
            });
        }
    }

    public class PackageConstructFromAlgorithm<F, T> : IProcessAlgorithm
        where T : class, IIdentifiable
        where F : class, IIdentifiable
    {
        public Enum OperationKey { get; private set; }

        public PackageConstructFromAlgorithm(Enum operationKey)
        {
            this.OperationKey = operationKey;
        }

        public virtual void Execute(IExecutingProcess executingProcess)
        {
            PackageDN package = (PackageDN)executingProcess.Data;

            executingProcess.ForEachLine(package, line =>
            {
                var result = OperationLogic.ConstructFromLite<T>(line.Entity.ToLite<F>(), OperationKey);
                if (result.IsNew)
                    result.Save();

                line.Result = result.ToLite<IIdentifiable>();
            });
        }
    }
}

