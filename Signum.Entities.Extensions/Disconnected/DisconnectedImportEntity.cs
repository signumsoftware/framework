using System;
using System.Linq;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Basics;

namespace Signum.Entities.Disconnected
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class DisconnectedImportEntity : Entity
    {
        public DateTime CreationDate { get; set; } = TimeZoneManager.Now;

        public Lite<DisconnectedMachineEntity>? Machine { get; set; }

        [Unit("ms")]
        public int? RestoreDatabase { get; set; }

        [Unit("ms")]
        public int? SynchronizeSchema { get; set; }

        [Unit("ms")]
        public int? DisableForeignKeys { get; set; }

        [PreserveOrder]
        public MList<DisconnectedImportTableEmbedded> Copies { get; set; } = new MList<DisconnectedImportTableEmbedded>();

        [Unit("ms")]
        public int? Unlock { get; set; }

        [Unit("ms")]
        public int? EnableForeignKeys { get; set; }

        [Unit("ms")]
        public int? DropDatabase { get; set; }

        [Unit("ms")]
        public int? Total { get; set; }

        public DisconnectedImportState State { get; set; }

        public Lite<ExceptionEntity>? Exception { get; set; }

        public double Ratio(DisconnectedImportEntity orientative)
        {
            double total = orientative.Total!.Value;

            double result = 0;

            if ((RestoreDatabase.HasValue || SynchronizeSchema.HasValue) && orientative.RestoreDatabase.HasValue) //Optional
                result += (orientative.RestoreDatabase.Value) / total;

            if (!SynchronizeSchema.HasValue)
                return result;
            result += (orientative.SynchronizeSchema!.Value) / total;

            if (!DisableForeignKeys.HasValue)
                return result;
            result += (orientative.DisableForeignKeys!.Value) / total;

            result += Copies.Where(c => c.CopyTable.HasValue).Join(
                orientative.Copies.Where(o => o.CopyTable.HasValue && o.CopyTable.Value > 0),
                c => c.Type, o => o.Type, (c, o) => o.CopyTable!.Value / total).Sum();

            if (!Copies.All(a => a.CopyTable.HasValue))
                return result;

            if (!Unlock.HasValue)
                return result;
            result += (orientative.Unlock!.Value) / total;

            if (!EnableForeignKeys.HasValue)
                return result;
            result += (orientative.EnableForeignKeys!.Value) / total;

            if (!DropDatabase.HasValue)
                return result;
            result += (orientative.DropDatabase!.Value) / total;

            return result;
        }

        [AutoExpressionField]
        public int CalculateTotal() => As.Expression(() => 
                (RestoreDatabase!.Value) +
                (SynchronizeSchema!.Value) +
                (DisableForeignKeys!.Value) +
                (Copies.Sum(a => a.CopyTable!.Value)) +
                (Unlock!.Value) +
                (EnableForeignKeys!.Value) +
                (DropDatabase!.Value));
    }

    public enum DisconnectedImportState
    {
        InProgress,
        Completed,
        Error,
    }

    [Serializable]
    public class DisconnectedImportTableEmbedded : EmbeddedEntity
    {
        
        public Lite<TypeEntity> Type { get; set; }

        [Unit("ms")]
        public int? CopyTable { get; set; }

        public bool? DisableForeignKeys { get; set; }

        public int? InsertedRows { get; set; }

        public int? UpdatedRows { get; set; }

        public int? InsertedOrUpdated
        {
            get
            {
                return InsertedRows + UpdatedRows;
            }
        }
    }
}
