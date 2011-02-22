using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Data.SqlClient;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using Signum.Engine;
using System.Data;
using Signum.Engine.Maps;
using Signum.Engine.Properties;

namespace Signum.Engine
{
    internal static class Deleter
    {
        public static void Delete(Type type, int id)
        {
            Schema.Current.OnDeleting(type, new List<int> { id }); 

            SqlPreCommand comando = DeleteCommand(type, id);

            int result = (int)Executor.ExecuteScalar(comando.ToSimple());
        }

        internal static SqlPreCommand DeleteCommand(Type type, int id)
        {
            Table table = ConnectionScope.Current.Schema.Table(type);

            SqlPreCommand comando = SqlPreCommand.Combine(Spacing.Double,
                table.DeleteSql(id),
                SqlBuilder.SelectRowCount()
                );
            return comando;
        }

        public static void Delete(Type type, List<int> ids)
        {
            Schema.Current.OnDeleting(type, ids);

            SqlPreCommand comando = DeleteCommand(type, ids);

            int result = (int)Executor.ExecuteScalar(comando.ToSimple());

            if (result != ids.Count)
                throw new InvalidOperationException("Not all entities with Type '{0}' and Id(s) {1} removed".Formato(type.Name, ids.ToString(", ")));
       }

        internal static SqlPreCommand DeleteCommand(Type type, List<int> ids)
        {
            Table table = ConnectionScope.Current.Schema.Table(type);

            SqlPreCommand comando = SqlPreCommand.Combine(Spacing.Double,
                table.DeleteSql(ids),
                SqlBuilder.SelectRowCount());
            return comando;
        }
    }
}
