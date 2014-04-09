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
    [Serializable, EntityKind(EntityKind.Part, EntityData.Transactional)]
    public class PackageDN : IdentifiableEntity, IProcessDataDN
    {
        [SqlDbType(Size = 200)]
        string name;
        [StringLengthValidator(AllowNulls = true, Max = 200)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string operationArguments;
        public string OperationArguments
        {
            get { return operationArguments; }
            private set { Set(ref operationArguments, value); }
        }

        [HiddenProperty]
        public object[] OperationArgs
        {
            get { return OperationArguments.HasText() ? (object[])Serialization.FromString(OperationArguments) : null;}
            set { OperationArguments = value == null ? null : Serialization.ToString(value); }
        }

        public override string ToString()
        {
            return "Package {0}".Formato(Name);
        }
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class PackageOperationDN : PackageDN
    {
        OperationSymbol operation;
        public OperationSymbol Operation
        {
            get { return operation; }
            set { SetToStr(ref operation, value); }
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


    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class PackageLineDN : IdentifiableEntity, IProcessLineDataDN
    {
        [NotNullable]
        Lite<PackageDN> package;
        [NotNullValidator]
        public Lite<PackageDN> Package
        {
            get { return package; }
            set { Set(ref package, value); }
        }

        [NotNullable, ImplementedByAll]
        IdentifiableEntity target;
        [NotNullValidator]
        public IdentifiableEntity Target
        {
            get { return target; }
            set { Set(ref target, value); }
        }

        [ImplementedByAll]
        Lite<IdentifiableEntity> result;
        public Lite<IdentifiableEntity> Result //ConstructFrom only!
        {
            get { return result; }
            set { Set(ref result, value); }
        }

        DateTime? finishTime;
        public DateTime? FinishTime
        {
            get { return finishTime; }
            set { Set(ref finishTime, value); }
        }
    }
}
