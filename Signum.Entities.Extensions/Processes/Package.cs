using System;
using Signum.Utilities;

namespace Signum.Entities.Processes
{
    [Serializable, EntityKind(EntityKind.Part, EntityData.Transactional)]
    public class PackageEntity : Entity, IProcessDataEntity
    {
        [StringLengthValidator(Max = 200)]
        public string? Name { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public byte[]? OperationArguments { get; private set; }

        [HiddenProperty]
        public object?[]? OperationArgs
        {
            get { return OperationArguments != null ? (object?[])Serialization.FromBytes(OperationArguments) : null; }
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
        public OperationSymbol Operation { get; set; }

        public override string ToString()
        {
            return "Package {0} {1}".FormatWith(Operation, Name);
        }
    }

    [AutoInit]
    public static class PackageOperationProcess
    {
        public static ProcessAlgorithmSymbol PackageOperation;
    }


    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
    public class PackageLineEntity : Entity, IProcessLineDataEntity
    {   
        public Lite<PackageEntity> Package { get; set; }

        [ImplementedByAll]
        public Entity Target { get; set; }

        [ImplementedByAll]
        public Lite<Entity>? Result { get; set; } //ConstructFrom only!

        public DateTime? FinishTime { get; set; }
    }

    public enum PackageQuery
    {
        PackageLineLastProcess,
        PackageLastProcess,
        PackageOperationLastProcess,
    }
}
