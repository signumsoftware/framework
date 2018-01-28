using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Disconnected
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class DisconnectedImportEntity : Entity
    {
        public DateTime CreationDate { get; set; } = TimeZoneManager.Now;

        public Lite<DisconnectedMachineEntity> Machine { get; set; }

        [Unit("ms")]
        public int? RestoreDatabase { get; set; }

        [Unit("ms")]
        public int? SynchronizeSchema { get; set; }

        [Unit("ms")]
        public int? DisableForeignKeys { get; set; }

        [NotNullValidator, PreserveOrder]
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

        public Lite<ExceptionEntity> Exception { get; set; }

        public double Ratio(DisconnectedImportEntity orientative)
        {
            double total = orientative.Total.Value;

            double result = 0;

            if ((RestoreDatabase.HasValue || SynchronizeSchema.HasValue) && orientative.RestoreDatabase.HasValue) //Optional
                result += (orientative.RestoreDatabase.Value) / total;

            if (!SynchronizeSchema.HasValue)
                return result;
            result += (orientative.SynchronizeSchema.Value) / total;

            if (!DisableForeignKeys.HasValue)
                return result;
            result += (orientative.DisableForeignKeys.Value) / total;

            result += Copies.Where(c => c.CopyTable.HasValue).Join(
                orientative.Copies.Where(o => o.CopyTable.HasValue && o.CopyTable.Value > 0),
                c => c.Type, o => o.Type, (c, o) => o.CopyTable.Value / total).Sum();

            if (!Copies.All(a => a.CopyTable.HasValue))
                return result;

            if (!Unlock.HasValue)
                return result;
            result += (orientative.Unlock.Value) / total;

            if (!EnableForeignKeys.HasValue)
                return result;
            result += (orientative.EnableForeignKeys.Value) / total;

            if (!DropDatabase.HasValue)
                return result;
            result += (orientative.DropDatabase.Value) / total;

            return result;
        }

        static Expression<Func<DisconnectedImportEntity, int>> CalculateTotalExpression =
            stat => (stat.RestoreDatabase.Value) +
                (stat.SynchronizeSchema.Value) +
                (stat.DisableForeignKeys.Value) +
                (stat.Copies.Sum(a => a.CopyTable.Value)) +
                (stat.Unlock.Value) +
                (stat.EnableForeignKeys.Value) +
                (stat.DropDatabase.Value);
        [ExpressionField]
        public int CalculateTotal()
        {
            return CalculateTotalExpression.Evaluate(this);
        }
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
        [NotNullValidator]
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
