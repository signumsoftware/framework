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


                dqm.RegisterExpression((PackageDN p) => p.Lines());

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
            }
        }

        public static PackageDN CreateLines(this PackageDN package, IEnumerable<Lite<IIdentifiable>> lites)
        {
            package.Save();

            lites.GroupsOf(20).SelectMany(gr =>
                gr.RetrieveFromListOfLite().Select(entity => new PackageLineDN
                {
                    Package = package.ToLite(),
                    Target = (IdentifiableEntity)entity
                })).SaveList();

            return package;
        }

        public static PackageDN CreateLines(this PackageDN package, IEnumerable<IIdentifiable> entities)
        {
            package.Save();

            entities.Select(entity => new PackageLineDN
            {
                Package = package.ToLite(),
                Target = (IdentifiableEntity)entity
            }).SaveList();

            return package;
        }

        public static ProcessDN CreatePackageOperation(IEnumerable<Lite<IIdentifiable>> entities, Enum operationKey)
        {
            return CreatePackageOperation(entities, operationKey.ToEntity<OperationDN>());
        }

        public static ProcessDN CreatePackageOperation(IEnumerable<Lite<IIdentifiable>> entities, OperationDN operation)
        {
            return ProcessLogic.Create(PackageOperationProcess.PackageOperation, new PackageOperationDN()
            {
                Operation = operation
            }.CreateLines(entities));
        }

        public static void RegisterUserTypeCondition(SchemaBuilder sb, Enum conditionName)
        {
            TypeConditionLogic.Register<UserProcessSessionDN>(conditionName,
               se => se.User.RefersTo(UserDN.Current));

            TypeConditionLogic.Register<ProcessDN>(conditionName,
                pe => ((UserProcessSessionDN)pe.Session).InCondition(conditionName));

            TypeConditionLogic.Register<PackageOperationDN>(conditionName,
                po => Database.Query<ProcessDN>().WhereCondition(conditionName).Any(pe => pe.Data == po));

            TypeConditionLogic.Register<PackageLineDN>(conditionName,
                pl => ((PackageOperationDN)pl.Package.Entity).InCondition(conditionName));
        }
    }

    public class PackageOperationAlgorithm : IProcessAlgorithm
    {
        public void Execute(ExecutingProcess executingProcess)
        {
            PackageOperationDN package = (PackageOperationDN)executingProcess.Data;

            Enum operationKey = package.Operation.ToEnum();

            executingProcess.ForEachLine(package.Lines().Where(a => a.FinishTime == null), line =>
            {
                OperationType operationType = OperationLogic.OperationType(line.Target.GetType(), operationKey);

                switch (operationType)
                {
                    case OperationType.Execute:
                        OperationLogic.Execute(line.Target, operationKey);
                        break;
                    case OperationType.Delete:
                        OperationLogic.Delete(line.Target, operationKey);
                        break;
                    case OperationType.ConstructorFrom:
                        {
                            var result = OperationLogic.ConstructFrom<IdentifiableEntity>(line.Target, operationKey);
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
        public Enum OperationKey { get; private set; }

        public PackageDeleteAlgorithm(Enum operationKey)
        {
            if (operationKey == null)
                throw new ArgumentNullException("operatonKey");

            this.OperationKey = operationKey;
        }

        public virtual void Execute(ExecutingProcess executingProcess)
        {
            PackageDN package = (PackageDN)executingProcess.Data;

            executingProcess.ForEachLine(package.Lines().Where(a => a.FinishTime == null), line =>
            {
                line.Target.Delete(OperationKey);

                line.FinishTime = TimeZoneManager.Now;
                line.Save();
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

        public virtual void Execute(ExecutingProcess executingProcess)
        {
            PackageDN package = (PackageDN)executingProcess.Data;

            executingProcess.ForEachLine(package.Lines().Where(a => a.FinishTime == null), line =>
            {
                line.Target.Execute(OperationKey);
                line.FinishTime = TimeZoneManager.Now;
                line.Save();
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

        public virtual void Execute(ExecutingProcess executingProcess)
        {
            PackageDN package = (PackageDN)executingProcess.Data;

            executingProcess.ForEachLine(package.Lines(), line =>
            {
                var result = line.Target.ConstructFrom<T>(OperationKey);
                if (result.IsNew)
                    result.Save();

                line.Result = ((IdentifiableEntity)(IIdentifiable)result).ToLite();

                line.FinishTime = TimeZoneManager.Now;
                line.Save();
            });
        }
    }
}

