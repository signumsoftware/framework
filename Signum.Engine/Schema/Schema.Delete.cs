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
        public SqlPreCommand DeleteSqlSync(Entity ident, string comment = null)
        {
            var pre = OnPreDeleteSqlSync(ident);
            var collections = (from tml in this.TablesMList()
                               select new SqlPreCommandSimple("DELETE {0} WHERE {1} = {2} --{3}"
                                   .FormatWith(tml.Name, tml.BackReference.Name.SqlEscape(), ident.Id, comment ?? ident.ToString()))).Combine(Spacing.Simple);

            var main = new SqlPreCommandSimple("DELETE {0} WHERE {1} = {2} --{3}"
                    .FormatWith(Name, this.PrimaryKey.Name.SqlEscape(), ident.Id, comment ?? ident.ToString()));

            return SqlPreCommand.Combine(Spacing.Simple, pre, collections, main);
        }

        public event Func<Entity, SqlPreCommand> PreDeleteSqlSync;

        SqlPreCommand OnPreDeleteSqlSync(Entity ident)
        {
            if (PreDeleteSqlSync == null)
                return null;

            return PreDeleteSqlSync.GetInvocationListTyped().Select(a => a(ident)).Combine(Spacing.Simple);
        }
    }
}
