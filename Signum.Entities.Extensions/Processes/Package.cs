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
    public class PackageEntity : Entity, IProcessDataEntity
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
        byte[] operationArguments;
        public byte[] OperationArguments
        {
            get { return operationArguments; }
            private set { Set(ref operationArguments, value); }
        }

        [HiddenProperty]
        public object[] OperationArgs
        {
            get { return OperationArguments != null ? (object[])Serialization.FromBytes(OperationArguments) : null;}
            set { OperationArguments = value == null ? null : Serialization.ToBytes(value); }
        }

        public override string ToString()
        {
            return "Package {0}".FormatWith(Name);
        }
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class PackageOperationEntity : PackageEntity
    {
        OperationSymbol operation;
        public OperationSymbol Operation
        {
            get { return operation; }
            set { SetToStr(ref operation, value); }
        }

        public override string ToString()
        {
            return "Package {0} {1}".FormatWith(Operation, Name); ;
        }
    }

    public static class PackageOperationProcess
    {
        public static readonly ProcessAlgorithmSymbol PackageOperation = new ProcessAlgorithmSymbol();
    }


    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
    public class PackageLineEntity : Entity, IProcessLineDataEntity
    {
        [NotNullable]
        Lite<PackageEntity> package;
        [NotNullValidator]
        public Lite<PackageEntity> Package
        {
            get { return package; }
            set { Set(ref package, value); }
        }

        [NotNullable, ImplementedByAll]
        Entity target;
        [NotNullValidator]
        public Entity Target
        {
            get { return target; }
            set { Set(ref target, value); }
        }

        [ImplementedByAll]
        Lite<Entity> result;
        public Lite<Entity> Result //ConstructFrom only!
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
