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
        public SqlPreCommand DeleteSqlSync(IdentifiableEntity ident, string comment = null)
        {
            var pre = OnPreDeleteSqlSync(ident);
            var collections = (from m in this.Fields
                               let ml = m.Value.Field as FieldMList
                               where ml != null
                               select new SqlPreCommandSimple("DELETE {0} WHERE {1} = {2} --{3}"
                                   .Formato(ml.RelationalTable.Name, ml.RelationalTable.BackReference.Name.SqlScape(), ident.Id, comment ?? ident.ToString()))).Combine(Spacing.Simple);

            var main = new SqlPreCommandSimple("DELETE {0} WHERE {1} = {2} --{3}"
                    .Formato(Name, "Id", ident.Id, comment ?? ident.ToString()));

            return SqlPreCommand.Combine(Spacing.Simple, pre, collections, main);
        }

        public event Func<IdentifiableEntity, SqlPreCommand> PreDeleteSqlSync;

        SqlPreCommand OnPreDeleteSqlSync(IdentifiableEntity ident)
        {
            if (PreDeleteSqlSync == null)
                return null;

            return PreDeleteSqlSync.GetInvocationList().Cast<Func<IdentifiableEntity, SqlPreCommand>>().Select(a => a(ident)).Combine(Spacing.Simple);
        }
    }
}
