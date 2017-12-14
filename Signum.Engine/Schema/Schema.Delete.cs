using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Signum.Entities;
using Signum.Engine;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Engine.Linq;

namespace Signum.Engine.Maps
{
    public partial class Table
    {
        public SqlPreCommand DeleteSqlSync<T>(T entity, Expression<Func<T, bool>> where, string comment = null)
            where T : Entity
        {
            if (typeof(T) != Type && where != null)
                throw new InvalidOperationException("Invalid table");

            var declaration = where != null ? DeclarePrimaryKeyVariable(entity, where) : null;

            var variableOrId = entity.Id.VariableName ?? entity.Id.Object;

            var pre = OnPreDeleteSqlSync(entity);
            var collections = (from tml in this.TablesMList()
                               select new SqlPreCommandSimple("DELETE {0} WHERE {1} = {2} --{3}"
                                   .FormatWith(tml.Name, tml.BackReference.Name.SqlEscape(), variableOrId, comment ?? entity.ToString()))).Combine(Spacing.Simple);

            var main = new SqlPreCommandSimple("DELETE {0} WHERE {1} = {2} --{3}"
                    .FormatWith(Name, this.PrimaryKey.Name.SqlEscape(), variableOrId, comment ?? entity.ToString()));

            return SqlPreCommand.Combine(Spacing.Simple, declaration, pre, collections, main);
        }

        int parameterIndex;
        private SqlPreCommand DeclarePrimaryKeyVariable<T>(T entity, Expression<Func<T, bool>> where) where T : Entity
        {
            var query = DbQueryProvider.Single.GetMainSqlCommand(Database.Query<T>().Where(where).Select(a => a.Id).Expression);
            
            string variableName = SqlParameterBuilder.GetParameterName(this.Name.Name + "Id_" + (parameterIndex++));
            entity.SetId(new Entities.PrimaryKey(entity.id.Value.Object, variableName));

            string queryString = query.PlainSql().Lines().ToString(" ");
            
            var result = new SqlPreCommandSimple($"DECLARE {variableName} {SqlBuilder.GetColumnType(this.PrimaryKey)}; SET {variableName} = COALESCE(({queryString}), 1 / 0)");

            return result;
        }

        public event Func<Entity, SqlPreCommand> PreDeleteSqlSync;

        SqlPreCommand OnPreDeleteSqlSync(Entity entity)
        {
            if (PreDeleteSqlSync == null)
                return null;

            return PreDeleteSqlSync.GetInvocationListTyped().Select(a => a(entity)).Combine(Spacing.Simple);
        }
    }
}
