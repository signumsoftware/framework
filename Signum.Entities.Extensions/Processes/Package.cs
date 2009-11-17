using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Operations;
using Signum.Utilities;

namespace Signum.Entities.Processes
{
    [Serializable, LocDescription]
    public class PackageDN : IdentifiableEntity, IProcessDataDN
    {
        [ SqlDbType(Size = 200)]
        string name;
        [StringLengthValidator(AllowNulls = true , Max = 200)]
        public string Name
        {
            get { return name; }
            set { Set(ref name, value, () => Name); }
        }

        OperationDN operation;
        public OperationDN Operation
        {
            get { return operation; }
            set { Set(ref operation, value, () => Operation); }
        }

        int numLines;
        public int NumLines
        {
            get { return numLines; }
            set { Set(ref numLines, value, () => NumLines); }
        }

        int numErrors;
        public int NumErrors
        {
            get { return numErrors; }
            set { Set(ref numErrors, value, () => NumErrors); }
        }

        public override string ToString()
        {
            return "{0} {1} ({2} lines{3})".Formato(Operation, Name, numLines, numErrors == 0 ? "" : ", {0} errors".Formato(numErrors));
        }
    }

    [Serializable, LocDescription]
    public class PackageLineDN : IdentifiableEntity
    {
        Lite<PackageDN> package;
        [LocDescription]
        public Lite<PackageDN> Package
        {
            get { return package; }
            set { Set(ref package, value, () => Package); }
        }

        [ImplementedByAll]
        Lite<IIdentifiable> target;
        [LocDescription]
        public Lite<IIdentifiable> Target
        {
            get { return target; }
            set { Set(ref target, value, () => Target); }
        }

        [ImplementedByAll]
        Lite<IIdentifiable> result;
        [LocDescription]
        public Lite<IIdentifiable> Result //ConstructFrom only!
        {
            get { return result; }
            set { Set(ref result, value, () => Result); }
        }

        DateTime? finishTime;
        [LocDescription]
        public DateTime? FinishTime
        {
            get { return finishTime; }
            set { Set(ref finishTime, value, () => FinishTime); }
        }


        [SqlDbType(Size = int.MaxValue)]
        string exception;
        [LocDescription]
        public string Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }
    }
}
