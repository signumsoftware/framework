using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Exceptions;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Entities.Disconnected
{
    [Serializable]
    public class DisconnectedExportDN : IdentifiableEntity
    {
        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { Set(ref creationDate, value, () => CreationDate); }
        }

        Lite<DisconnectedMachineDN> machine;
        [NotNullValidator]
        public Lite<DisconnectedMachineDN> Machine
        {
            get { return machine; }
            set { Set(ref machine, value, () => Machine); }
        }

        long? @lock;
        [Unit("ms")]
        public long? Lock
        {
            get { return @lock; }
            set { Set(ref @lock, value, () => Lock); }
        }

        long? createDatabase;
        [Unit("ms")]
        public long? CreateDatabase
        {
            get { return createDatabase; }
            set { Set(ref createDatabase, value, () => CreateDatabase); }
        }

        long? createSchema;
        [Unit("ms")]
        public long? CreateSchema
        {
            get { return createSchema; }
            set { Set(ref createSchema, value, () => CreateSchema); }
        }

        long? disableForeignKeys;
        [Unit("ms")]
        public long? DisableForeignKeys
        {
            get { return disableForeignKeys; }
            set { Set(ref disableForeignKeys, value, () => DisableForeignKeys); }
        }

        MList<DisconnectedExportTableDN> copies = new MList<DisconnectedExportTableDN>();
        public MList<DisconnectedExportTableDN> Copies
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

        long? reseedIds;
        [Unit("ms")]
        public long? ReseedIds
        {
            get { return reseedIds; }
            set { Set(ref reseedIds, value, () => ReseedIds); }
        }
        
        long? backupDatabase;
        [Unit("ms")]
        public long? BackupDatabase
        {
            get { return backupDatabase; }
            set { Set(ref backupDatabase, value, () => BackupDatabase); }
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

        DisconnectedExportState state;
        public DisconnectedExportState State
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

        public double Ratio(DisconnectedExportDN estimation)
        {
            double total = (long)estimation.Total.Value;

            double result = 0;

            if (!CreateDatabase.HasValue)
                return result;
            result += (estimation.CreateDatabase.Value) / total;

            if (!CreateSchema.HasValue)
                return result;
            result += (estimation.CreateSchema.Value) / total;

            if (!DisableForeignKeys.HasValue)
                return result;
            result += (estimation.DisableForeignKeys.Value) / total;


            result += Copies.Where(c => c.CopyTable.HasValue).Join(
                estimation.Copies.Where(o => o.CopyTable.HasValue && o.CopyTable.Value > 0),
                c => c.Type, o => o.Type, (c, o) => o.CopyTable.Value / total).Sum();

            if (!Copies.All(a => a.CopyTable.HasValue))
                return result;

            if (!EnableForeignKeys.HasValue)
                return result;
            result += (estimation.EnableForeignKeys.Value) / total;

            if (!ReseedIds.HasValue)
                return result;
            result += (estimation.ReseedIds.Value) / total;

            if (!BackupDatabase.HasValue)
                return result;
            result += (estimation.BackupDatabase.Value) / total;

            if (!DropDatabase.HasValue)
                return result;
            result += (estimation.DropDatabase.Value) / total;

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

        static Expression<Func<DisconnectedExportDN, long>> CalculateTotalExpression =
            stat => (stat.CreateDatabase ?? 0) +
                (stat.CreateSchema ?? 0) +
                (stat.DisableForeignKeys ?? 0) +
                (stat.Copies.Sum(a => a.CopyTable ?? 0)) +
                (stat.EnableForeignKeys ?? 0) +
                (stat.ReseedIds ?? 0) +
                (stat.BackupDatabase ?? 0) +
                (stat.DropDatabase ?? 0); 
        public long CalculateTotal()
        {
            return CalculateTotalExpression.Evaluate(this);
        }
    }

    public enum DisconnectedExportState
    {
        InProgress,
        Completed,
        Error,
    }

    [Serializable]
    public class DisconnectedExportTableDN : EmbeddedEntity
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

        [SqlDbType(Size = int.MaxValue)]
        string errors;
        public string Errors
        {
            get { return errors; }
            set { Set(ref errors, value, () => Errors); }
        }

        int order;
        public int Order
        {
            get { return order; }
            set { Set(ref order, value, () => Order); }
        }
    }
}
