using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Entities.Basics;

namespace Signum.Entities.Disconnected
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class DisconnectedExportEntity : Entity
    {
        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { Set(ref creationDate, value); }
        }

        Lite<DisconnectedMachineEntity> machine;
        [NotNullValidator]
        public Lite<DisconnectedMachineEntity> Machine
        {
            get { return machine; }
            set { Set(ref machine, value); }
        }

        int? @lock;
        [Unit("ms")]
        public int? Lock
        {
            get { return @lock; }
            set { Set(ref @lock, value); }
        }

        int? createDatabase;
        [Unit("ms")]
        public int? CreateDatabase
        {
            get { return createDatabase; }
            set { Set(ref createDatabase, value); }
        }

        int? createSchema;
        [Unit("ms")]
        public int? CreateSchema
        {
            get { return createSchema; }
            set { Set(ref createSchema, value); }
        }

        int? disableForeignKeys;
        [Unit("ms")]
        public int? DisableForeignKeys
        {
            get { return disableForeignKeys; }
            set { Set(ref disableForeignKeys, value); }
        }

        [NotNullable, PreserveOrder]
        MList<DisconnectedExportTableEntity> copies = new MList<DisconnectedExportTableEntity>();
        public MList<DisconnectedExportTableEntity> Copies
        {
            get { return copies; }
            set { Set(ref copies, value); }
        }

        int? enableForeignKeys;
        [Unit("ms")]
        public int? EnableForeignKeys
        {
            get { return enableForeignKeys; }
            set { Set(ref enableForeignKeys, value); }
        }

        int? reseedIds;
        [Unit("ms")]
        public int? ReseedIds
        {
            get { return reseedIds; }
            set { Set(ref reseedIds, value); }
        }

        int? backupDatabase;
        [Unit("ms")]
        public int? BackupDatabase
        {
            get { return backupDatabase; }
            set { Set(ref backupDatabase, value); }
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

        DisconnectedExportState state;
        public DisconnectedExportState State
        {
            get { return state; }
            set { Set(ref state, value); }
        }

        Lite<ExceptionEntity> exception;
        public Lite<ExceptionEntity> Exception
        {
            get { return exception; }
            set { Set(ref exception, value); }
        }

        public double Ratio(DisconnectedExportEntity estimation)
        {
            double total = (long)estimation.Total.Value;
 
            double result = 0;

            if (!Lock.HasValue)
                return result;
            result += (estimation.Lock.Value) / total;
            
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

        static Expression<Func<DisconnectedExportEntity, int>> CalculateTotalExpression =
            stat =>
                stat.Lock.Value +
                stat.CreateDatabase.Value +
                stat.CreateSchema.Value +
                stat.DisableForeignKeys.Value +
                stat.Copies.Sum(a => a.CopyTable.Value) +
                stat.EnableForeignKeys.Value +
                stat.ReseedIds.Value +
                stat.BackupDatabase.Value +
                stat.DropDatabase.Value;
        public int CalculateTotal()
        {
            return CalculateTotalExpression.Evaluate(this);
        }

        internal DisconnectedExportEntity Clone()
        {
            return new DisconnectedExportEntity
            {
                Machine = machine,
                Lock = Lock,
                CreateDatabase = CreateDatabase,
                DisableForeignKeys = DisableForeignKeys,
                Copies = Copies.Select(c=>new DisconnectedExportTableEntity
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
    public class DisconnectedExportTableEntity : EmbeddedEntity
    {
        Lite<TypeEntity> type;
        [NotNullValidator]
        public Lite<TypeEntity> Type
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

        [SqlDbType(Size = int.MaxValue)]
        string errors;
        public string Errors
        {
            get { return errors; }
            set { Set(ref errors, value); }
        }
    }
}
