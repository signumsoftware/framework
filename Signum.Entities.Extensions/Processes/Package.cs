using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Operations;

namespace Signum.Entities.Processes
{
    [Serializable]
    public class PackageDN : IdentifiableEntity, IProcessData
    {
        OperationDN operation;
        public OperationDN Operation
        {
            get { return operation; }
            set { Set(ref operation, value, "Operation"); }
        }
    }

    [Serializable]
    public class PackageLineDN : IdentifiableEntity
    {
        Lazy<PackageDN> package;
        public Lazy<PackageDN> Package
        {
            get { return package; }
            set { Set(ref package, value, "Package"); }
        }

        [ImplementedByAll]
        Lazy<IdentifiableEntity> entity;
        public Lazy<IdentifiableEntity> Entity
        {
            get { return entity; }
            set { Set(ref entity, value, "Entity"); }
        }

        DateTime? finishTime;
        public DateTime? FinishTime
        {
            get { return finishTime; }
            set { Set(ref finishTime, value, "FinishTime"); }
        }


        [SqlDbType(Size = int.MaxValue)]
        string exception;
        public string Exception
        {
            get { return exception; }
            set { Set(ref exception, value, "Exception"); }
        }
    }
}
