using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Exceptions;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Entities.Disconnected
{
    [Serializable]
    public class DisconnectedImportDN : IdentifiableEntity
    {
        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { Set(ref creationDate, value, () => CreationDate); }
        }

        Lite<DisconnectedMachineDN> machine;
        public Lite<DisconnectedMachineDN> Machine
        {
            get { return machine; }
            set { Set(ref machine, value, () => Machine); }
        }

        int? restoreDatabase;
        [Unit("ms")]
        public int? RestoreDatabase
        {
            get { return restoreDatabase; }
            set { Set(ref restoreDatabase, value, () => RestoreDatabase); }
        }

        int? synchronizeSchema;       
        [Unit("ms")]
        public int? SynchronizeSchema
        {
            get { return synchronizeSchema; }
            set { Set(ref synchronizeSchema, value, () => SynchronizeSchema); }
        }

        int? disableForeignKeys;
        [Unit("ms")]
        public int? DisableForeignKeys
        {
            get { return disableForeignKeys; }
            set { Set(ref disableForeignKeys, value, () => DisableForeignKeys); }
        }

        MList<DisconnectedImportTableDN> copies = new MList<DisconnectedImportTableDN>();
        public MList<DisconnectedImportTableDN> Copies
        {
            get { return copies; }
            set { Set(ref copies, value, () => Copies); }
        }

        int? unlock;
        [Unit("ms")]
        public int? Unlock
        {
            get { return unlock; }
            set { Set(ref unlock, value, () => Unlock); }
        }

        int? enableForeignKeys;
        [Unit("ms")]
        public int? EnableForeignKeys
        {
            get { return enableForeignKeys; }
            set { Set(ref enableForeignKeys, value, () => EnableForeignKeys); }
        }

        int? dropDatabase;
        [Unit("ms")]
        public int? DropDatabase
        {
            get { return dropDatabase; }
            set { Set(ref dropDatabase, value, () => DropDatabase); }
        }

        int? total;
        [Unit("ms")]
        public int? Total
        {
            get { return total; }
            set { Set(ref total, value, () => Total); }
        }

        DisconnectedImportState state;
        public DisconnectedImportState State
        {
            get { return state; }
            set { Set(ref state, value, () => State); }
        }

        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
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

        protected override void PreSaving(ref bool graphModified)
        {
            if (Copies != null)
                copies.ForEach((a, i) => a.Order = i);

            base.PreSaving(ref graphModified);
        }

        protected override void PostRetrieving()
        {
            copies.Sort(a => a.Order);
            base.PostRetrieving();
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
            set { Set(ref type, value, () => Type); }
        }

        int? copyTable;
        [Unit("ms")]
        public int? CopyTable
        {
            get { return copyTable; }
            set { Set(ref copyTable, value, () => CopyTable); }
        }

        bool? disableForeignKeys;
        public bool? DisableForeignKeys
        {
            get { return disableForeignKeys; }
            set { Set(ref disableForeignKeys, value, () => DisableForeignKeys); }
        }

        int? insertedRows;
        public int? InsertedRows
        {
            get { return insertedRows; }
            set { Set(ref insertedRows, value, () => InsertedRows); }
        }

        int? updatedRows;
        public int? UpdatedRows
        {
            get { return updatedRows; }
            set { Set(ref updatedRows, value, () => UpdatedRows); }
        }

        public int? InsertedOrUpdated
        {
            get
            {
                return InsertedRows + UpdatedRows;
            }
        }

        int order;
        public int Order
        {
            get { return order; }
            set { Set(ref order, value, () => Order); }
        }
    }
}
