using System;
using System.Linq;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Entities.Basics;

namespace Signum.Entities.Disconnected
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class DisconnectedExportEntity : Entity
    {
        public DateTime CreationDate { get; set; } = TimeZoneManager.Now;

        
        public Lite<DisconnectedMachineEntity> Machine { get; set; }

        [Unit("ms")]
        public int? Lock { get; set; }

        [Unit("ms")]
        public int? CreateDatabase { get; set; }

        [Unit("ms")]
        public int? CreateSchema { get; set; }

        [Unit("ms")]
        public int? DisableForeignKeys { get; set; }

        [PreserveOrder]
        public MList<DisconnectedExportTableEmbedded> Copies { get; set; } = new MList<DisconnectedExportTableEmbedded>();

        [Unit("ms")]
        public int? EnableForeignKeys { get; set; }

        [Unit("ms")]
        public int? ReseedIds { get; set; }

        [Unit("ms")]
        public int? BackupDatabase { get; set; }

        [Unit("ms")]
        public int? DropDatabase { get; set; }

        [Unit("ms")]
        public int? Total { get; set; }

        public DisconnectedExportState State { get; set; }

        public Lite<ExceptionEntity>? Exception { get; set; }

        public double Ratio(DisconnectedExportEntity estimation)
        {
            double total = (long)estimation.Total!.Value;

            double result = 0;

            if (Lock != null)
                return result;
            result += (estimation.Lock!.Value) / total;

            if (!CreateDatabase.HasValue)
                return result;
            result += (estimation.CreateDatabase!.Value) / total;

            if (!CreateSchema.HasValue)
                return result;
            result += (estimation.CreateSchema!.Value) / total;

            if (!DisableForeignKeys.HasValue)
                return result;
            result += (estimation.DisableForeignKeys!.Value) / total;


            result += Copies.Where(c => c.CopyTable.HasValue).Join(
                estimation.Copies.Where(o => o.CopyTable.HasValue && o.CopyTable.Value > 0),
                c => c.Type, o => o.Type, (c, o) => o.CopyTable!.Value / total).Sum();

            if (!Copies.All(a => a.CopyTable.HasValue))
                return result;

            if (!EnableForeignKeys.HasValue)
                return result;
            result += (estimation.EnableForeignKeys!.Value) / total;

            if (!ReseedIds.HasValue)
                return result;
            result += (estimation.ReseedIds!.Value) / total;

            if (!BackupDatabase.HasValue)
                return result;
            result += (estimation.BackupDatabase!.Value) / total;

            if (!DropDatabase.HasValue)
                return result;
            result += (estimation.DropDatabase!.Value) / total;

            return result;
        }

        [AutoExpressionField]
        public int CalculateTotal() => As.Expression(() => 
                Lock!.Value +
                CreateDatabase!.Value +
                CreateSchema!.Value +
                DisableForeignKeys!.Value +
                Copies.Sum(a => a.CopyTable!.Value) +
                EnableForeignKeys!.Value +
                ReseedIds!.Value +
                BackupDatabase!.Value +
                DropDatabase!.Value);

        internal DisconnectedExportEntity Clone()
        {
            return new DisconnectedExportEntity
            {
                Machine = Machine,
                Lock = Lock,
                CreateDatabase = CreateDatabase,
                DisableForeignKeys = DisableForeignKeys,
                Copies = Copies.Select(c => new DisconnectedExportTableEmbedded
                {
                    Type = c.Type,
                    CopyTable = c.CopyTable,
                    Errors = c.Errors,
                }).ToMList(),
                EnableForeignKeys = EnableForeignKeys,
                ReseedIds = ReseedIds,
                BackupDatabase = BackupDatabase,
                DropDatabase = DropDatabase,
                Total = Total,
                State = State,
                Exception = Exception
            };
        }
    }

    public enum DisconnectedExportState
    {
        InProgress,
        Completed,
        Error,
    }

    [Serializable]
    public class DisconnectedExportTableEmbedded : EmbeddedEntity
    {
        
        public Lite<TypeEntity> Type { get; set; }

        [Unit("ms")]
        public int? CopyTable { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string Errors { get; set; }
    }
}
