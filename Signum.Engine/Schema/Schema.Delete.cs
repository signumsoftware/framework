using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Signum.Entities;
using Signum.Engine;
using Signum.Utilities;

namespace Signum.Engine.Maps
{
    public partial class Table
    {
        public SqlPreCommand DeleteSql(int id)
        {
            SqlParameter pid = SqlParameterBuilder.CreateIdParameter(id);

            var collectionFields = Fields.Values.Select(a => a.Field).OfType<FieldMList>();

            return SqlPreCommand.Combine(Spacing.Simple,
                        collectionFields.Select(c => c.RelationalTable.DeleteSql(pid)).Combine(Spacing.Simple),
                        SqlBuilder.DeleteSql(Name, pid));
        }

        public SqlPreCommand DeleteSql(List<int> ids)
        {
            List<SqlParameter> pids = ids.Select((id, i) => SqlParameterBuilder.CreateReferenceParameter(SqlBuilder.PrimaryKeyName + i, false, id)).ToList();

            var collectionFields = Fields.Values.Select(a => a.Field).OfType<FieldMList>();

            return SqlPreCommand.Combine(Spacing.Simple,
                        collectionFields.Select(c => c.RelationalTable.DeleteSql(pids)).Combine(Spacing.Simple),
                        SqlBuilder.DeleteSql(Name, pids));
        }

        public SqlPreCommand DeleteSqlSync(IdentifiableEntity ident, string comment = null)
        {
            return SqlPreCommand.Combine(Spacing.Simple,
                OnPreDeleteSqlSync(ident),
                SqlBuilder.DeleteSqlSync(Name, ident.Id, comment ?? ident.ToStr));
        }

        public event Func<IdentifiableEntity, SqlPreCommand> PreDeleteSqlSync;

        SqlPreCommand OnPreDeleteSqlSync(IdentifiableEntity ident)
        {
            if (PreDeleteSqlSync == null)
                return null;

            return PreDeleteSqlSync.GetInvocationList().Cast<Func<IdentifiableEntity, SqlPreCommand>>().Select(a => a(ident)).Combine(Spacing.Simple);
        }
    }

    public partial class RelationalTable
    {
        internal SqlPreCommand DeleteSql(SqlParameter pid)
        {
            return SqlBuilder.RelationalDelete(Name, BackReference.Name, pid);         
        }

        internal SqlPreCommand DeleteSql(List<SqlParameter> pids)
        {
            return SqlBuilder.RelationalDelete(Name, BackReference.Name, pids);
        }
    }

}
