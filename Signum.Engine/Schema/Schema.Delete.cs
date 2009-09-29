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
            var collectionFields = Fields.Values.Select(a=>a.Field).OfType<FieldMList>();

            return SqlPreCommand.Combine(Spacing.Simple, 
                        SqlBuilder.RestoreLastId(id),  
                        collectionFields.Select(c=>c.RelationalTable.DeleteSql()).Combine(Spacing.Simple),
                        SqlBuilder.DeleteSql(Name));
        }

        public SqlPreCommand DeleteSqlSync(IdentifiableEntity ident)
        {
            return SqlBuilder.DeleteSql(Name, ident.Id, ident.ToStr);
        }
    }

    public partial class RelationalTable
    {
        internal SqlPreCommand DeleteSql()
        {
            return SqlBuilder.RelationalDeleteScope(Name, BackReference.Name);         
        }
    }

}
