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
    public class PackageLogic : IProcessLogic
    {
        OperationDN operation;

        public PackageLogic(OperationDN operation)
        {
            this.operation = operation;
        }

        public PackageLogic(Enum operationKey)
        {
            this.operation = EnumBag<OperationDN>.ToEntity(operationKey);
        }

        public virtual IProcessData CreateData(object[] args)
        {
            PackageDN package = new PackageDN { Operation = operation };
            package.Save();

            List<Lazy> lazies = (List<Lazy>)args[0];

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

            Enum key = EnumBag<OperationDN>.ToEnum(package.Operation.Key);

            List<Lazy<PackageLineDN>> lines =
                (from pl in Database.Query<PackageLineDN>()
                 where pl.Package == package.ToLazy() && pl.FinishTime == null && pl.Error == null
                 select pl.ToLazy()).ToList();


            int lastPercentage = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                if (executingProcess.Suspended)
                    return FinalState.Suspended;

                PackageLineDN pl = lines[i].Retrieve();

                try
                {
                    using (Transaction tr = new Transaction(true))
                    {
                        OperationLogic.ServiceExecuteLazy(pl.Target, key);
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

