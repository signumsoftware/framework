using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data;
using System.IO;
using Microsoft.SqlServer.Server;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine;

namespace Signum.Engine.Maps
{
    public partial class Table
    {
        internal SqlPreCommand CreateCollectionTables()
        {
            var collecciones = Fields.Select(c => c.Value).OfType<CollectionField>();

            return collecciones.Select(c => c.RelationalTable.CreateTableTotal()).Combine(Spacing.Double);
        }
    }

    public partial class RelationalTable
    {
        public SqlPreCommand CreateTableTotal()
        {
            return SqlPreCommand.Combine(Spacing.Double,
                SqlBuilder.CreateTableSql(this),
                SqlBuilder.AlterTableForeignKeys(this),
                SqlBuilder.CreateIndicesSql(this));
        }
    }
}
