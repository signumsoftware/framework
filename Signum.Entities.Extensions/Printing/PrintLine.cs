using System;
using Signum.Entities.Processes;
using Signum.Entities.Files;
using System.Reflection;
using Signum.Entities.Authorization;
using Signum.Entities.Scheduler;

namespace Signum.Entities.Printing
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class PrintLineEntity : Entity, IProcessLineDataEntity
    {
        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        [Ignore]
        public FileTypeSymbol TestFileType { get; set; }

        
        public FilePathEmbedded File { get; set; }

        public Lite<PrintPackageEntity>? Package { get; set; }

        public DateTime? PrintedOn { get; set; }

        [ImplementedBy()]
        public Lite<Entity> Referred { get; set; }

        public PrintLineState State { get; set; }

        static StateValidator<PrintLineEntity, PrintLineState> stateValidator =
            new StateValidator<PrintLineEntity, PrintLineState>
            (n => n.State, n => n.PrintedOn, n=>n.Package)
            {
                { PrintLineState.NewTest,           false, false },
                { PrintLineState.ReadyToPrint,      false, false },
                { PrintLineState.Enqueued,          false, true  },
                { PrintLineState.Printed,           true,  null  },
                { PrintLineState.Error,             false, null  },
                { PrintLineState.Cancelled,         false, null  },
                { PrintLineState.PrintedAndDeleted, true,  null  }
            };
        protected override string? PropertyValidation(PropertyInfo pi)
        {
            return stateValidator.Validate(this, pi) ?? base.PropertyValidation(pi);
        }
    }
    public enum PrintLineState
    {
        NewTest,
        ReadyToPrint,
        Enqueued,
        Printed,
        Cancelled,
        Error,
        PrintedAndDeleted
    }

    [AutoInit]
    public static class PrintLineOperation
    {
        public static ConstructSymbol<PrintLineEntity>.Simple CreateTest;
        public static ExecuteSymbol<PrintLineEntity> SaveTest;
        public static ExecuteSymbol<PrintLineEntity> Print;
        public static ExecuteSymbol<PrintLineEntity> Retry;
        public static ExecuteSymbol<PrintLineEntity> Cancel;
    }

    [AutoInit]
    public static class PrintPackageProcess
    {
        public static readonly ProcessAlgorithmSymbol PrintPackage;
    }

    [AutoInit]
    public static class PrintPermission
    {
        public static PermissionSymbol ViewPrintPanel;
    }

    [AutoInit]
    public static class PrintTask
    {
        public static SimpleTaskSymbol RemoveOldFiles;
    }

}
