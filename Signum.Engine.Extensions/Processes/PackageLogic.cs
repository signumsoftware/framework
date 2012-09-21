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

                dqm[typeof(PackageDN)] =
                     (from p in Database.Query<PackageDN>()
                      select new
                      {
                          Entity = p,
                          p.Id,
                          p.Operation,
                          p.Name ,
                          Lines = (int?)Database.Query<PackageLineDN>().Count(pl => pl.Package == p.ToLite())
                      }).ToDynamic();

                dqm[typeof(PackageLineDN)] =
                    (from pl in Database.Query<PackageLineDN>()
                     select new
                     {
                         Entity = pl,
                         Package = pl.Package,
                         pl.Id,
                         pl.Target,
                         pl.FinishTime,
                         pl.Exception,
                     }).ToDynamic();
            }
        }
    }

    public abstract class PackageAlgorithm<T>: IProcessAlgorithm
        where T:class, IIdentifiable
    {
        Func<List<Lite<T>>> getLites;

        public PackageAlgorithm()
        {
        }

        public PackageAlgorithm(Func<List<Lite<T>>> getLites)
        {
            this.getLites = getLites;
        }

        public virtual IProcessDataDN CreateData(object[] args)
        {
            PackageDN package = CreatePackage(args);

            package.Save();

            List<Lite<T>> lites = 
                args != null && args.Length > 1? (List<Lite<T>>)args.GetArg<List<Lite<T>>>(1): 
                getLites != null? getLites(): null;

            if (lites == null)
                throw new InvalidOperationException("No entities to process found");

            package.NumLines = lites.Count; 
            
            lites.Select(lite => new PackageLineDN
            {
                Package = package.ToLite(),
                Target = lite.ToLite<IIdentifiable>()
            }).SaveList();

            return package;
        }


        public virtual void Execute(IExecutingProcess executingProcess)
        {
            PackageDN package = (PackageDN)executingProcess.Data;

            List<Lite<PackageLineDN>> lines =
                (from pl in Database.Query<PackageLineDN>()
                 where pl.Package == package.ToLite() && pl.FinishTime == null && pl.Exception == null
                 select pl.ToLite()).ToList();

            for (int i = 0; i < lines.Count; i++)
            {
                executingProcess.CancellationToken.ThrowIfCancellationRequested();
                
                PackageLineDN pl = lines[i].RetrieveAndForget();

                try
                {
                    using (Transaction tr = Transaction.ForceNew())
                    {
                        ExecuteLine(pl, package);
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

                    package.NumErrors++;
                    package.Save();
                }

                executingProcess.ProgressChanged(i, lines.Count);
            }
        }

        public int NotificationSteps = 100; 
        protected abstract PackageDN CreatePackage(object[] args);
        public abstract void ExecuteLine(PackageLineDN pl, PackageDN package);
    }

    public class PackageExecuteAlgorithm<T> : PackageAlgorithm<T> where T : class, IIdentifiable
    {
        public Enum OperationKey { get; private set; }

        public PackageExecuteAlgorithm(Enum operationKey)
        {
            this.OperationKey = operationKey;
        }

        public PackageExecuteAlgorithm(Enum operationKey, Func<List<Lite<T>>> getLites)
            : base(getLites)
        {
            this.OperationKey = operationKey;
        }

        public override void ExecuteLine(PackageLineDN pl, PackageDN package)
        {
            pl.Target.ToLite<T>().ExecuteLite<T>(OperationKey);
        }

        protected override PackageDN CreatePackage(object[] args)
        {
            PackageDN package = new PackageDN { Operation = EnumLogic<OperationDN>.ToEntity(OperationKey) };

            if (args != null && args.Length > 0)
                package.Name = args.TryGetArgC<string>(0);
            return package;
        }
    }

    public class PackageConstructFromAlgorithm<F, T> : PackageAlgorithm<F>
        where T : class, IIdentifiable
        where F : class, IIdentifiable
    {
        public Enum OperationKey { get; private set; }

        public PackageConstructFromAlgorithm(Enum operationKey)
        {
            this.OperationKey = operationKey;
        }

        public PackageConstructFromAlgorithm(Enum operationKey, Func<List<Lite<F>>> getLites)
            : base(getLites)
        {
            this.OperationKey = operationKey;
        }

        public override void ExecuteLine(PackageLineDN pl, PackageDN package)
        {
            var result = OperationLogic.ConstructFromLite<T>(pl.Target.ToLite<F>(), OperationKey);
            if (result.IsNew)
                result.Save();

            pl.Result = result.ToLite<IIdentifiable>();
        }

        protected override PackageDN CreatePackage(object[] args)
        {
            PackageDN package = new PackageDN { Operation = EnumLogic<OperationDN>.ToEntity(OperationKey) };

            if (args != null && args.Length > 0)
                package.Name = args.TryGetArgC<string>(0);
            return package;
        }
    }
}

