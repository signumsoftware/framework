using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;

namespace Signum.Entities.Processes
{
    [Serializable, EntityType(EntityType.Part)]
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

    [Serializable, EntityType(EntityType.System)]
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

    [Serializable, EntityType(EntityType.System)]
    public class PackageLineDN : IdentifiableEntity
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
        Lite<IIdentifiable> entity;
        [NotNullValidator]
        public Lite<IIdentifiable> Entity
        {
            get { return entity; }
            set { Set(ref entity, value, () => Entity); }
        }

        [ImplementedByAll]
        Lite<IIdentifiable> result;
        public Lite<IIdentifiable> Result //ConstructFrom only!
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

        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }
    }
}
