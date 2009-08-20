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

namespace Signum.Engine.Processes
{
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

