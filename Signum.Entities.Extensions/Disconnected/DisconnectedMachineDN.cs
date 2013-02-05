using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using System.Linq.Expressions;
using Signum.Utilities;
using System.ComponentModel;

namespace Signum.Entities.Disconnected
{
    [Serializable, EntityKind(EntityKind.Main)]
    public class DisconnectedMachineDN : Entity
    {
        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value, () => CreationDate); }
        }

        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string machineName;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string MachineName
        {
            get { return machineName; }
            set { Set(ref machineName, value, () => MachineName); }
        }

        DisconnectedMachineState state;
        public DisconnectedMachineState State
        {
            get { return state; }
            set { Set(ref state, value, () => State); }
        }

        int seedMin;
        public int SeedMin
        {
            get { return seedMin; }
            set { Set(ref seedMin, value, () => SeedMin); }
        }

        int seedMax;
        public int SeedMax
        {
            get { return seedMax; }
            set { Set(ref seedMax, value, () => SeedMax); }
        }

        static Expression<Func<DisconnectedMachineDN, string>> ToStringExpression = e => e.machineName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public static readonly SessionVariable<Lite<DisconnectedMachineDN>> CurrentVariable = 
            Statics.SessionVariable<Lite<DisconnectedMachineDN>>("disconectedMachine");
        public static Lite<DisconnectedMachineDN> Current
        {
            get { return CurrentVariable.Value; }
            set { CurrentVariable.Value = value; }
        }
    }

    public enum DisconnectedMachineState
    {
        Connected,
        Disconnected,
        Faulted,
        Fixed,
    }

    public enum DisconnectedMachineOperation
    {
        Save,
        UnsafeUnlock,
        FixImport,
    }

    public interface IDisconnectedEntity : IIdentifiable
    {
        long Ticks { get; set; }
        long? LastOnlineTicks { get; set; }
        Lite<DisconnectedMachineDN> DisconnectedMachine { get; set; }
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
        The0WithId12IsLockedBy3
    }
}
