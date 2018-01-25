using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using System.Linq.Expressions;
using Signum.Utilities;
using System.ComponentModel;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Disconnected
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class DisconnectedMachineEntity : Entity
    {
        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        [UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string MachineName { get; set; }

        public DisconnectedMachineState State { get; set; }

        [NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int SeedMin { get; set; }

        [NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int SeedMax { get; set; }

        static Expression<Func<DisconnectedMachineEntity, Interval<int>>> SeedIntervalExpression =
            entity => new Interval<int>(entity.SeedMin, entity.SeedMax);
        [HiddenProperty, ExpressionField]
        public Interval<int> SeedInterval
        {
            get { return SeedIntervalExpression.Evaluate(this); }
        }

        static Expression<Func<DisconnectedMachineEntity, string>> ToStringExpression = e => e.MachineName;
        [ExpressionField]
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
            if (pi.Name == nameof(SeedMax) && SeedMax <= SeedMin)
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

    [AutoInit]
    public static class DisconnectedMachineOperation
    {
        public static ExecuteSymbol<DisconnectedMachineEntity> Save;
        public static ExecuteSymbol<DisconnectedMachineEntity> UnsafeUnlock;
        public static ConstructSymbol<DisconnectedImportEntity>.From<DisconnectedMachineEntity> FixImport;
    }

    [Serializable]
    public class DisconnectedCreatedMixin : MixinEntity
    {
        DisconnectedCreatedMixin(Entity mainEntity, MixinEntity next) : base(mainEntity, next) { }

        public bool DisconnectedCreated { get; set; }
    }

    [Serializable]
    public class DisconnectedSubsetMixin : MixinEntity
    {
        DisconnectedSubsetMixin(Entity mainEntity, MixinEntity next) : base(mainEntity, next) { }

        public long? LastOnlineTicks { get; set; }

        public Lite<DisconnectedMachineEntity> DisconnectedMachine { get; set; }
    }

    [Serializable]
    public class StrategyPair
    {
        public Download Download;
        public Upload Upload;
    }


    [InTypeScript(true)]
    public enum Download
    {
        None,
        All,
        Subset,
        Replace,
    }

    [InTypeScript(true)]
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
