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

        long? restoreDatabase;
        [Unit("ms")]
        public long? RestoreDatabase
        {
            get { return restoreDatabase; }
            set { Set(ref restoreDatabase, value, () => RestoreDatabase); }
        }

        long? synchronizeSchema;       
        [Unit("ms")]
        public long? SynchronizeSchema
        {
            get { return synchronizeSchema; }
            set { Set(ref synchronizeSchema, value, () => SynchronizeSchema); }
        }

        long? disableForeignKeys;
        [Unit("ms")]
        public long? DisableForeignKeys
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

        long? enableForeignKeys;
        [Unit("ms")]
        public long? EnableForeignKeys
        {
            get { return enableForeignKeys; }
            set { Set(ref enableForeignKeys, value, () => EnableForeignKeys); }
        }

        long? dropDatabase;
        [Unit("ms")]
        public long? DropDatabase
        {
            get { return dropDatabase; }
            set { Set(ref dropDatabase, value, () => DropDatabase); }
        }

        long? total;
        [Unit("ms")]
        public long? Total
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
            double total =
                (orientative.RestoreDatabase ?? 0) +
                (orientative.SynchronizeSchema ?? 0) +
                (orientative.DisableForeignKeys ?? 0) +
                (orientative.Copies.Sum(a => a.CopyTable ?? 0)) +
                (orientative.EnableForeignKeys ?? 0) +
                (orientative.DropDatabase ?? 0);

            double result = 0;

            if (!RestoreDatabase.HasValue)
                return result;
            result += (orientative.RestoreDatabase.Value) / total;

            if (!SynchronizeSchema.HasValue)
                return result;
            result += (orientative.SynchronizeSchema.Value) / total;

            result += Copies.Where(c => c.CopyTable.HasValue).Join(
                orientative.Copies.Where(o => o.CopyTable.HasValue && o.CopyTable.Value > 0),
                c => c.Type, o => o.Type, (c, o) => o.CopyTable.Value / total).Sum();

            if (!Copies.All(a => a.CopyTable.HasValue))
                return result;

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

        static Expression<Func<DisconnectedImportDN, long>> CalculateTotalExpression =
            stat => (stat.RestoreDatabase ?? 0) +
                (stat.Copies.Sum(a => a.CopyTable ?? 0));
        public long CalculateTotal()
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

        long? copyTable;
        [Unit("ms")]
        public long? CopyTable
        {
            get { return copyTable; }
            set { Set(ref copyTable, value, () => CopyTable); }
        }

        bool? foreignKeysDisabled;
        public bool? DisableForeignKeys
        {
            get { return foreignKeysDisabled; }
            set { Set(ref foreignKeysDisabled, value, () => DisableForeignKeys); }
        }

        int? inserted;
        public int? Inserted
        {
            get { return inserted; }
            set { Set(ref inserted, value, () => Inserted); }
        }

        int? updated;
        public int? Updated
        {
            get { return updated; }
            set { Set(ref updated, value, () => Updated); }
        }

        public int? InsertedOrUpdated
        {
            get
            {
                return Inserted + Updated;
            }
        }

        int order;
        public int Order
        {
            get { return order; }
            set { Set(ref order, value, () => Order); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string errors;
        public string Errors
        {
            get { return errors; }
            set { Set(ref errors, value, () => Errors); }
        }
    }
}
