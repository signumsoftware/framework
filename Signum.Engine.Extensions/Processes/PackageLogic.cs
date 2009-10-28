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
                          Entity = p.ToLazy(),
                          p.Id,
                          Operation = p.Operation.ToLazy(),
                          Lines = (int?)Database.Query<PackageLineDN>().Count(pl => pl.Package == p.ToLazy())
                      }).ToDynamic();

                dqm[typeof(PackageLineDN)] =
                    (from pl in Database.Query<PackageLineDN>()
                     select new
                     {
                         Entity = pl.ToLazy(),
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

    public class PackageAlgorithm: IProcessAlgorithm
    {
        Enum operationKey;

        Func<List<Lazy>> getLazies;

        public PackageAlgorithm(Enum operationKey)
        {
            this.operationKey = operationKey;
        }

        public PackageAlgorithm(Enum operationKey, Func<List<Lazy>> getLazies)
        {
            this.operationKey = operationKey;
            this.getLazies = getLazies;
        }

        public virtual IProcessDataDN CreateData(object[] args)
        {
            PackageDN package = new PackageDN { Operation = EnumLogic<OperationDN>.ToEntity(operationKey) };
            package.Save();


            List<Lazy> lazies = 
                args != null && args.Length > 0? (List<Lazy>)args[0]: 
                getLazies != null? getLazies(): null;

            if (lazies == null)
                throw new ApplicationException("No entities to process found");

            package.NumLines = lazies.Count; 
            
            lazies.Select(lazy => new PackageLineDN
            {
                Package = package.ToLazy(),
                Target = lazy.ToLazy<IdentifiableEntity>()
            }).SaveList();

            return package;
        }

        public FinalState Execute(IExecutingProcess executingProcess)
        {
            PackageDN package = (PackageDN)executingProcess.Data;

            List<Lazy<PackageLineDN>> lines =
                (from pl in Database.Query<PackageLineDN>()
                 where pl.Package == package.ToLazy() && pl.FinishTime == null && pl.Exception == null
                 select pl.ToLazy()).ToList();

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
                        OperationLogic.ServiceExecuteLazy(pl.Target, operationKey);
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
    }
}

