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
            var collectionFields = Fields.Values.OfType<CollectionField>();

            return SqlPreCommand.Combine(Spacing.Simple, 
                        SqlBuilder.RestoreLastId(id),  
                        collectionFields.Select(c=>c.RelationalTable.DeleteSql()).Combine(Spacing.Simple),
                        SqlBuilder.DeleteSql(Name));
        }

        public SqlPreCommand DeleteSqlSync(int id)
        {
            return SqlBuilder.DeleteSql(Name, id);
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
