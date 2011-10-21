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
            return SqlPreCommand.Combine(Spacing.Simple,
                OnPreDeleteSqlSync(ident),
                new SqlPreCommandSimple("DELETE {0} WHERE id = {1} --{2}".Formato(Name.SqlScape(), ident.Id, comment ?? ident.ToStr))
                );
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
