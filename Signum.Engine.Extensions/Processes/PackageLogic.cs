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

namespace Signum.Engine.Processes
{
    public static class PackageLogic
    {
        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null, null)));
        }

        internal static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                ProcessLogic.AssertStarted(sb);

                sb.Include<PackageDN>();
                sb.Include<PackageLineDN>();

                OperationLogic.Register(new BasicExecute<ProcessDN>(TaskOperation.ExecutePrivate)
                {
                    Execute = (pc, _) => ProcessLogic.Create(pc).Execute(ProcessOperation.Execute)
                });

                dqm[typeof(PackageDN)] =
                     (from p in Database.Query<PackageDN>()
                      select new
                      {
                          Entity = p.ToLite(),
                          p.Id,
                          Operation = p.Operation.ToLite(),
                          p.Name ,
                          Lines = (int?)Database.Query<PackageLineDN>().Count(pl => pl.Package == p.ToLite())
                      }).ToDynamic();

                dqm[typeof(PackageLineDN)] =
                    (from pl in Database.Query<PackageLineDN>()
                     select new
                     {
                         Entity = pl.ToLite(),
                         Package = pl.Package,
                         pl.Id,
                         pl.Target,
                         pl.FinishTime,
                         pl.Exception
                     }).ToDynamic()
                     .ChangeColumn(a => a.Package, c => c.Visible = false)
                     .ChangeColumn(a => a.Target, c => c.Filterable = false);
            }
        }

    }

    public abstract class PackageAlgorithm<T>: IProcessAlgorithm
        where T:class, IIdentifiable
    {
        public Enum OperationKey { get; private set; }

        Func<List<Lite<T>>> getLazies;

        public PackageAlgorithm(Enum operationKey)
        {
            this.OperationKey = operationKey;
        }

        public PackageAlgorithm(Enum operationKey, Func<List<Lite<T>>> getLazies)
        {
            this.OperationKey = operationKey;
            this.getLazies = getLazies;
        }

        public virtual IProcessDataDN CreateData(object[] args)
        {
            PackageDN package = new PackageDN { Operation = EnumLogic<OperationDN>.ToEntity(OperationKey) };
            package.Save();

            List<Lite<T>> lites = 
                args != null && args.Length > 0? (List<Lite<T>>)args[0]: 
                getLazies != null? getLazies(): null;

            if (lites == null)
                throw new ApplicationException("No entities to process found");

            package.NumLines = lites.Count; 
            
            lites.Select(lite => new PackageLineDN
            {
                Package = package.ToLite(),
                Target = lite.ToLite<IIdentifiable>()
            }).SaveList();

            return package;
        }

        public FinalState Execute(IExecutingProcess executingProcess)
        {
            PackageDN package = (PackageDN)executingProcess.Data;

            List<Lite<PackageLineDN>> lines =
                (from pl in Database.Query<PackageLineDN>()
                 where pl.Package == package.ToLite() && pl.FinishTime == null && pl.Exception == null
                 select pl.ToLite()).ToList();

            int lastPercentage = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                if (executingProcess.Suspended)
                    return FinalState.Suspended;

                PackageLineDN pl = lines[i].RetrieveAndForget();

                try
                {
                    using (Transaction tr = new Transaction(true))
                    {
                        ExecuteLine(pl);
                        pl.FinishTime = DateTime.Now;
                        pl.Save();
                        tr.Commit();
                    }
                }
                catch (Exception e)
                {
                    using (Transaction tr = new Transaction(true))
                    {
                        pl.Exception = e.Message;
                        pl.Save();
                        tr.Commit();

                        package.NumErrors++;
                        package.Save();
                    }
                }

                int percentage = (100 * i) / lines.Count;
                if (percentage != lastPercentage)
                {
                    executingProcess.ProgressChanged(percentage);
                    lastPercentage = percentage;
                }
            }

            return FinalState.Finished;
        }

        public abstract void ExecuteLine(PackageLineDN pl);
    }

    public class PackageExecuteAlgorithm<T> : PackageAlgorithm<T> where T:class, IIdentifiable
    {
        public PackageExecuteAlgorithm(Enum operationKey)
            : base(operationKey)
        {
        }

        public PackageExecuteAlgorithm(Enum operationKey, Func<List<Lite<T>>> getLazies):base(operationKey, getLazies)
        {
        }

        public override void ExecuteLine(PackageLineDN pl)
        {
            OperationLogic.ExecuteLite<T>(pl.Target.ToLite<T>(), OperationKey);
        }
    }

    public class PackageConstructFromAlgorithm<F, T> : PackageAlgorithm<F>
        where T : class, IIdentifiable
        where F : class, IIdentifiable
    {
        public PackageConstructFromAlgorithm(Enum operationKey)
            : base(operationKey)
        {
        }

        public PackageConstructFromAlgorithm(Enum operationKey, Func<List<Lite<F>>> getLazies):base(operationKey, getLazies)
        {
        }

        public override void ExecuteLine(PackageLineDN pl)
        {
            var result = OperationLogic.ConstructFromLite<T>(pl.Target.ToLite<F>(), OperationKey);
            if (result.IsNew)
                result.Save();

            pl.Result = result.ToLite<IIdentifiable>();
        }
    }
}

