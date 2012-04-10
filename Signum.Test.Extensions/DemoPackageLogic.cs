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
using System.Threading;

namespace Signum.Engine.Processes
{
    public static class DemoPackageLogic
    {
        static Expression<Func<DemoPackageDN, IQueryable<DemoPackageLineDN>>> LinesExpression =
            p => Database.Query<DemoPackageLineDN>().Where(pl => pl.Package.RefersTo(p));
        public static IQueryable<DemoPackageLineDN> Lines(this DemoPackageDN p)
        {
            return LinesExpression.Evaluate(p);
        }


        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                ProcessLogic.AssertStarted(sb);

                sb.Include<DemoPackageDN>();
                sb.Include<DemoPackageLineDN>();

                dqm[typeof(DemoPackageDN)] =
                     (from p in Database.Query<DemoPackageDN>()
                      select new
                      {
                          Entity = p,
                          p.Id,
                          p.Name,
                          Lines = (int?)p.Lines().Count()
                      }).ToDynamic();

                dqm[typeof(DemoPackageLineDN)] =
                    (from pl in Database.Query<DemoPackageLineDN>()
                     select new
                     {
                         Entity = pl,
                         Package = pl.Package,
                         pl.Id,
                         pl.FinishTime,
                         pl.Exception,
                     }).ToDynamic();

                dqm.RegisterExpression((DemoPackageDN p) => p.Lines());

                new BasicConstructFrom<DemoPackageDN, ProcessExecutionDN>(DemoPackageOperations.CreateProcess)
                {
                    Construct = (dp, _) =>
                    {
                        0.To(dp.RequestedLines).Select(i => new DemoPackageLineDN
                        {
                            Package = dp.ToLite(),
                        }).SaveList();

                        return ProcessLogic.Create(DemoPackageProcess.DemoProcess, dp);
                    }
                }.Register();

                ProcessLogic.Register(DemoPackageProcess.DemoProcess, new DemoPackageAlgorithm());
            }
        }
    }

    public class DemoPackageAlgorithm: IProcessAlgorithm
    {
        public DemoPackageAlgorithm()
        {
        }

        public virtual IProcessDataDN CreateData(object[] args)
        {
            throw new InvalidOperationException();
        }

        public void Execute(IExecutingProcess executingProcess)
        {
            DemoPackageDN package = (DemoPackageDN)executingProcess.Data;

            List<Lite<DemoPackageLineDN>> lines =
                (from pl in package.Lines()
                 where pl.FinishTime == null && pl.Exception == null
                 select pl.ToLite()).ToList();

            Random r = new Random();

            for (int i = 0; i < lines.Count; i++)
            {
                executingProcess.CancellationToken.ThrowIfCancellationRequested();

                DemoPackageLineDN pl = lines[i].RetrieveAndForget();
                try
                {
                    Thread.Sleep(package.DelayMilliseconds);

                    if (r.NextDouble() < package.ErrorRate)
                        throw new NotSupportedException("Random exception on demo line {0}".Formato(pl.Id));

                    pl.FinishTime = TimeZoneManager.Now;

                    pl.Save();
                }
                catch (Exception e)
                {
                    if (Transaction.AvoidIndependentTransactions)
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
    }
}

