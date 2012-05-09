using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Exceptions;
using System.Linq.Expressions;

namespace Signum.Entities.Disconnected
{
    [Serializable]
    public class DownloadStatisticsDN : IdentifiableEntity
    {
        DateTime creationDate;
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

        long? unlock;
        [Format("ms")]
        public long? Unlock
        {
            get { return unlock; }
            set { Set(ref unlock, value, () => Unlock); }
        }

        long? createDatabase;
        [Format("ms")]
        public long? CreateDatabase
        {
            get { return createDatabase; }
            set { Set(ref createDatabase, value, () => CreateDatabase); }
        }

        long? createSchema;
        [Format("ms")]
        public long? CreateSchema
        {
            get { return createSchema; }
            set { Set(ref createSchema, value, () => CreateSchema); }
        }

        long? disableForeignKeys;
        [Format("ms")]
        public long? DisableForeignKeys
        {
            get { return disableForeignKeys; }
            set { Set(ref disableForeignKeys, value, () => DisableForeignKeys); }
        }

        MList<DownloadStatisticsTableDN> copies = new MList<DownloadStatisticsTableDN>();
        public MList<DownloadStatisticsTableDN> Copies
        {
            get { return copies; }
            set { Set(ref copies, value, () => Copies); }
        }

        long? enableForeignKeys;
        [Format("ms")]
        public long? EnableForeignKeys
        {
            get { return enableForeignKeys; }
            set { Set(ref enableForeignKeys, value, () => EnableForeignKeys); }
        }

        long? reseedIds;
        [Format("ms")]
        public long? ReseedIds
        {
            get { return reseedIds; }
            set { Set(ref reseedIds, value, () => ReseedIds); }
        }
        
        long? backupDatabase;
        [Format("ms")]
        public long? BackupDatabase
        {
            get { return backupDatabase; }
            set { Set(ref backupDatabase, value, () => BackupDatabase); }
        }

        long? dropDatabase;
        [Format("ms")]
        public long? DropDatabase
        {
            get { return dropDatabase; }
            set { Set(ref dropDatabase, value, () => DropDatabase); }
        }

        long? total;
        [Format("ms")]
        public long? Total
        {
            get { return total; }
            set { Set(ref total, value, () => Total); }
        }

        DownloadStatisticsState state;
        public DownloadStatisticsState State
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

        public double Ratio(DownloadStatisticsDN estimation)
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

        static Expression<Func<DownloadStatisticsDN, long>> CalculateTotalExpression =
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

    public enum DownloadStatisticsState
    {
        InProgress,
        Completed,
        Error,
    }

    [Serializable]
    public class DownloadStatisticsTableDN : EmbeddedEntity
    {
        Lite<TypeDN> type;
        [NotNullValidator]
        public Lite<TypeDN> Type
        {
            get { return type; }
            set { Set(ref type, value, () => Type); }
        }

        long? copyTable;
        [Format("ms")]
        public long? CopyTable
        {
            get { return copyTable; }
            set { Set(ref copyTable, value, () => CopyTable); }
        }

        int order;
        public int Order
        {
            get { return order; }
            set { Set(ref order, value, () => Order); }
        }
    }
}
