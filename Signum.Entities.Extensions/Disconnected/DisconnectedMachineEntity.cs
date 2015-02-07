using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using System.Linq.Expressions;
using Signum.Utilities;
using System.ComponentModel;
using Signum.Utilities.DataStructures;

namespace Signum.Entities.Disconnected
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class DisconnectedMachineEntity : Entity
    {
        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value); }
        }

        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string machineName;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string MachineName
        {
            get { return machineName; }
            set { Set(ref machineName, value); }
        }

        DisconnectedMachineState state;
        public DisconnectedMachineState State
        {
            get { return state; }
            set { Set(ref state, value); }
        }

        int seedMin;
        [NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int SeedMin
        {
            get { return seedMin; }
            set { Set(ref seedMin, value); }
        }

        int seedMax;
        [NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int SeedMax
        {
            get { return seedMax; }
            set { Set(ref seedMax, value); }
        }

        static Expression<Func<DisconnectedMachineEntity, Interval<int>>> SeedIntervalExpression =
            entity => new Interval<int>(entity.SeedMin, entity.SeedMax);
        [HiddenProperty]
        public Interval<int> SeedInterval
        {
            get { return SeedIntervalExpression.Evaluate(this); }
        }

        static Expression<Func<DisconnectedMachineEntity, string>> ToStringExpression = e => e.machineName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public static readonly SessionVariable<Lite<DisconnectedMachineEntity>> CurrentVariable = 
            Statics.SessionVariable<Lite<DisconnectedMachineEntity>>("disconectedMachine");
        public static Lite<DisconnectedMachineEntity> Current
        {
            get { return CurrentVariable.Value; }
            set { CurrentVariable.Value = value; }
        }

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Is(() => SeedMax) && SeedMax <= SeedMin)
                return ValidationMessage._0ShouldBeGreaterThan1.NiceToString(pi, NicePropertyName(() => SeedMin));

            return base.PropertyValidation(pi);
        }

    }

    public enum DisconnectedMachineState
    {
        Connected,
        Disconnected,
        Faulted,
        Fixed,
    }

    public static class DisconnectedMachineOperation
    {
        public static readonly ExecuteSymbol<DisconnectedMachineEntity> Save = OperationSymbol.Execute<DisconnectedMachineEntity>();
        public static readonly ExecuteSymbol<DisconnectedMachineEntity> UnsafeUnlock = OperationSymbol.Execute<DisconnectedMachineEntity>();
        public static readonly ConstructSymbol<DisconnectedImportEntity>.From<DisconnectedMachineEntity> FixImport = OperationSymbol.Construct<DisconnectedImportEntity>.From<DisconnectedMachineEntity>();
    }

    [Serializable]
    public class DisconnectedCreatedMixin : MixinEntity
    {
        DisconnectedCreatedMixin(Entity mainEntity, MixinEntity next) : base(mainEntity, next) { }

        bool disconnectedCreated;
        public bool DisconnectedCreated
        {
            get { return disconnectedCreated; }
            set { Set(ref disconnectedCreated, value); }
        }
    }

    [Serializable]
    public class DisconnectedSubsetMixin : MixinEntity
    {
        DisconnectedSubsetMixin(Entity mainEntity, MixinEntity next) : base(mainEntity, next) { }

        long? lastOnlineTicks;
        public long? LastOnlineTicks
        {
            get { return lastOnlineTicks; }
            set { Set(ref lastOnlineTicks, value); }
        }

        Lite<DisconnectedMachineEntity> disconnectedMachine;
        public Lite<DisconnectedMachineEntity> DisconnectedMachine
        {
            get { return disconnectedMachine; }
            set { Set(ref disconnectedMachine, value); }
        }
    }

    [Serializable]
    public class StrategyPair
    {
        public Download Download;
        public Upload Upload;
    }

    
    public enum Download
    {
        None,
        All,
        Subset,
        Replace,
    }

    public enum Upload
    {
        None,
        New,
        Subset
    }

    public enum DisconnectedMessage
    {
        [Description("Not allowed to save {0} while offline")]
        NotAllowedToSave0WhileOffline,
        [Description("The {0} with Id {1} ({2}) is locked by {3}")]
        The0WithId12IsLockedBy3,
        Imports,
        Exports,
        [Description("{0} overlaps with {1}")]
        _0OverlapsWith1
    }
}
