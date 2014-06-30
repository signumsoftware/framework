using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Basics;

namespace Signum.Entities.Disconnected
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class DisconnectedImportDN : Entity
    {
        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { Set(ref creationDate, value); }
        }

        Lite<DisconnectedMachineDN> machine;
        public Lite<DisconnectedMachineDN> Machine
        {
            get { return machine; }
            set { Set(ref machine, value); }
        }

        int? restoreDatabase;
        [Unit("ms")]
        public int? RestoreDatabase
        {
            get { return restoreDatabase; }
            set { Set(ref restoreDatabase, value); }
        }

        int? synchronizeSchema;       
        [Unit("ms")]
        public int? SynchronizeSchema
        {
            get { return synchronizeSchema; }
            set { Set(ref synchronizeSchema, value); }
        }

        int? disableForeignKeys;
        [Unit("ms")]
        public int? DisableForeignKeys
        {
            get { return disableForeignKeys; }
            set { Set(ref disableForeignKeys, value); }
        }

        [NotNullable, PreserveOrder]
        MList<DisconnectedImportTableDN> copies = new MList<DisconnectedImportTableDN>();
        public MList<DisconnectedImportTableDN> Copies
        {
            get { return copies; }
            set { Set(ref copies, value); }
        }

        int? unlock;
        [Unit("ms")]
        public int? Unlock
        {
            get { return unlock; }
            set { Set(ref unlock, value); }
        }

        int? enableForeignKeys;
        [Unit("ms")]
        public int? EnableForeignKeys
        {
            get { return enableForeignKeys; }
            set { Set(ref enableForeignKeys, value); }
        }

        int? dropDatabase;
        [Unit("ms")]
        public int? DropDatabase
        {
            get { return dropDatabase; }
            set { Set(ref dropDatabase, value); }
        }

        int? total;
        [Unit("ms")]
        public int? Total
        {
            get { return total; }
            set { Set(ref total, value); }
        }

        DisconnectedImportState state;
        public DisconnectedImportState State
        {
            get { return state; }
            set { Set(ref state, value); }
        }

        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value); }
        }

        public double Ratio(DisconnectedImportDN orientative)
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

        static Expression<Func<DisconnectedImportDN, int>> CalculateTotalExpression =
            stat => (stat.RestoreDatabase.Value) +
                (stat.SynchronizeSchema.Value) +
                (stat.DisableForeignKeys.Value) +
                (stat.Copies.Sum(a => a.CopyTable.Value)) +
                (stat.Unlock.Value) +
                (stat.EnableForeignKeys.Value) +
                (stat.DropDatabase.Value);
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
    public class DisconnectedImportTableDN : EmbeddedEntity
    {
        Lite<TypeDN> type;
        [NotNullValidator]
        public Lite<TypeDN> Type
        {
            get { return type; }
            set { Set(ref type, value); }
        }

        int? copyTable;
        [Unit("ms")]
        public int? CopyTable
        {
            get { return copyTable; }
            set { Set(ref copyTable, value); }
        }

        bool? disableForeignKeys;
        public bool? DisableForeignKeys
        {
            get { return disableForeignKeys; }
            set { Set(ref disableForeignKeys, value); }
        }

        int? insertedRows;
        public int? InsertedRows
        {
            get { return insertedRows; }
            set { Set(ref insertedRows, value); }
        }

        int? updatedRows;
        public int? UpdatedRows
        {
            get { return updatedRows; }
            set { Set(ref updatedRows, value); }
        }

        public int? InsertedOrUpdated
        {
            get
            {
                return InsertedRows + UpdatedRows;
            }
        }
    }
}
