using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using System.Linq.Expressions;

namespace Signum.Entities.Processes
{
    [Serializable, EntityKind(EntityKind.Part)]
    public class PackageDN : IdentifiableEntity, IProcessDataDN
    {
        [SqlDbType(Size = 200)]
        string name;
        [StringLengthValidator(AllowNulls = true, Max = 200)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        public override string ToString()
        {
            return "Package {0}".Formato(Name);
        }
    }

    [Serializable, EntityKind(EntityKind.System)]
    public class PackageOperationDN : PackageDN
    {
        OperationDN operation;
        public OperationDN Operation
        {
            get { return operation; }
            set { SetToStr(ref operation, value, () => Operation); }
        }

        public override string ToString()
        {
            return "Package {0} {1}".Formato(Operation, Name); ;
        }
    }

    public enum PackageOperationProcess
    {
        PackageOperation
    }


    [Serializable, EntityKind(EntityKind.System)]
    public class PackageLineDN : IdentifiableEntity, IProcessLineDataDN
    {
        [NotNullable]
        Lite<PackageDN> package;
        [NotNullValidator]
        public Lite<PackageDN> Package
        {
            get { return package; }
            set { Set(ref package, value, () => Package); }
        }

        [NotNullable, ImplementedByAll]
        IdentifiableEntity target;
        [NotNullValidator]
        public IdentifiableEntity Target
        {
            get { return target; }
            set { Set(ref target, value, () => Target); }
        }

        [ImplementedByAll]
        Lite<IdentifiableEntity> result;
        public Lite<IdentifiableEntity> Result //ConstructFrom only!
        {
            get { return result; }
            set { Set(ref result, value, () => Result); }
        }

        DateTime? finishTime;
        public DateTime? FinishTime
        {
            get { return finishTime; }
            set { Set(ref finishTime, value, () => FinishTime); }
        }
    }
}
