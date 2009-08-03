using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Operations;
using Signum.Utilities;

namespace Signum.Entities.Processes
{
    [Serializable, LocDescription]
    public class PackageDN : IdentifiableEntity, IProcessData
    {
        OperationDN operation;
        public OperationDN Operation
        {
            get { return operation; }
            set { Set(ref operation, value, "Operation"); }
        }

        int numLines;
        public int NumLines
        {
            get { return numLines; }
            set { Set(ref numLines, value, "NumLines"); }
        }

        int numErrors;
        public int NumErrors
        {
            get { return numErrors; }
            set { Set(ref numErrors, value, "NumErrors"); }
        }

        public override string ToString()
        {
            return "Package ({0} lines{1})".Formato(numLines, numErrors == 0 ? "" : ", {0} errors".Formato(numErrors));
        }
    }

    [Serializable, LocDescription]
    public class PackageLineDN : IdentifiableEntity
    {
        Lazy<PackageDN> package;
        [LocDescription]
        public Lazy<PackageDN> Package
        {
            get { return package; }
            set { Set(ref package, value, "Package"); }
        }

        [ImplementedByAll]
        Lazy<IdentifiableEntity> target;
        [LocDescription]
        public Lazy<IdentifiableEntity> Target
        {
            get { return target; }
            set { Set(ref target, value, "Target"); }
        }

        DateTime? finishTime;
        [LocDescription]
        public DateTime? FinishTime
        {
            get { return finishTime; }
            set { Set(ref finishTime, value, "FinishTime"); }
        }


        [SqlDbType(Size = int.MaxValue)]
        string exception;
        [LocDescription]
        public string Exception
        {
            get { return exception; }
            set { Set(ref exception, value, "Exception"); }
        }
    }
}
