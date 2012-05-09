using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Exceptions;

namespace Signum.Entities.Disconnected
{
    [Serializable]
    public class UploadStatisticsDN : IdentifiableEntity
    {
        DateTime creationDate;
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

        MList<UploadTypeStatisticsDN> copies = new MList<UploadTypeStatisticsDN>();
        public MList<UploadTypeStatisticsDN> Copies
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

        long? reseedForeignKeys;
        [Format("ms")]
        public long? ReseedForegnKeys
        {
            get { return reseedForeignKeys; }
            set { Set(ref reseedForeignKeys, value, () => ReseedForegnKeys); }
        }
        
        long? createBackup;
        [Format("ms")]
        public long? CreateBackup
        {
            get { return createBackup; }
            set { Set(ref createBackup, value, () => CreateBackup); }
        }

        long? removeDatabase;
        [Format("ms")]
        public long? RemoveDatabase
        {
            get { return removeDatabase; }
            set { Set(ref removeDatabase, value, () => RemoveDatabase); }
        }

        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }

        public decimal Ratio(DownloadStatisticsDN orientative)
        {
            decimal total =
                (orientative.CreateDatabase ?? 0) +
                (orientative.CreateSchema ?? 0) +
                (orientative.DisableForeignKeys ?? 0) +
                (orientative.Copies.Sum(a => a.CopyTable ?? 0)) +
                (orientative.EnableForeignKeys ?? 0) +
                (orientative.BackupDatabase ?? 0) +
                (orientative.DropDatabase ?? 0);

            decimal result = 0;

            if (!CreateDatabase.HasValue)
                return result;
            result += (orientative.CreateDatabase.Value) / total;

            if (!CreateSchema.HasValue)
                return result;
            result += (orientative.CreateSchema.Value) / total;

            if (!DisableForeignKeys.HasValue)
                return result;
            result += (orientative.DisableForeignKeys.Value) / total;


            result += Copies.Where(c => c.CopyTable.HasValue).Join(
                orientative.Copies.Where(o => o.CopyTable.HasValue && o.CopyTable.Value > 0),
                c => c.Type, o => o.Type, (c, o) => o.CopyTable.Value / total).Sum();

            if (!Copies.All(a => a.CopyTable.HasValue))
                return result;

            if (!EnableForeignKeys.HasValue)
                return result;
            result += (orientative.EnableForeignKeys.Value) / total;

            if (!CreateBackup.HasValue)
                return result;
            result += (orientative.BackupDatabase.Value) / total;

            if (!RemoveDatabase.HasValue)
                return result;
            result += (orientative.DropDatabase.Value) / total;

            return result;
        }
    }

    [Serializable]
    public class UploadTypeStatisticsDN : EmbeddedEntity
    {
        Lite<TypeDN> type;
        [NotNullValidator]
        public Lite<TypeDN> Type
        {
            get { return type; }
            set { Set(ref type, value, () => Type); }
        }

        long? copyTable;
        public long? CopyTable
        {
            get { return copyTable; }
            set { Set(ref copyTable, value, () => CopyTable); }
        }
    }
}
