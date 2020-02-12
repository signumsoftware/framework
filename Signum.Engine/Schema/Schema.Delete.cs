using System;
using System.Linq;
using Signum.Entities;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Engine.Linq;

namespace Signum.Engine.Maps
{
    public partial class Table
    {
        public SqlPreCommand DeleteSqlSync<T>(T entity, Expression<Func<T, bool>>? where, string? comment = null)
            where T : Entity
        {
            if (typeof(T) != Type && where != null)
                throw new InvalidOperationException("Invalid table");

            var declaration = where != null ? DeclarePrimaryKeyVariable(entity, where) : null;

            var variableOrId = entity.Id.VariableName ?? entity.Id.Object;
            var isPostgres = Schema.Current.Settings.IsPostgres;
            var pre = OnPreDeleteSqlSync(entity);
            var collections = (from tml in this.TablesMList()
                               select new SqlPreCommandSimple("DELETE FROM {0} WHERE {1} = {2}; --{3}"
                                   .FormatWith(tml.Name, tml.BackReference.Name.SqlEscape(isPostgres), variableOrId, comment ?? entity.ToString()))).Combine(Spacing.Simple);

            var main = new SqlPreCommandSimple("DELETE FROM {0} WHERE {1} = {2}; --{3}"
                    .FormatWith(Name, this.PrimaryKey.Name.SqlEscape(isPostgres), variableOrId, comment ?? entity.ToString()));

            if (isPostgres && declaration != null)
                return PostgresDoBlock(entity.Id.VariableName!, declaration, SqlPreCommand.Combine(Spacing.Simple, pre, collections, main)!);

            return SqlPreCommand.Combine(Spacing.Simple, declaration, pre, collections, main)!;
        }

        int parameterIndex;
        private SqlPreCommandSimple DeclarePrimaryKeyVariable<T>(T entity, Expression<Func<T, bool>> where) where T : Entity
        {
            var query = DbQueryProvider.Single.GetMainSqlCommand(Database.Query<T>().Where(where).Select(a => a.Id).Expression);

            string variableName = this.Name.Name + "Id_" + (parameterIndex++);
            if (!Schema.Current.Settings.IsPostgres)
                variableName = SqlParameterBuilder.GetParameterName(variableName);

            entity.SetId(new Entities.PrimaryKey(entity.id!.Value.Object, variableName));

            string queryString = query.PlainSql().Lines().ToString(" ");

            var result = Schema.Current.Settings.IsPostgres ?
            new SqlPreCommandSimple(@$"{variableName} {Connector.Current.SqlBuilder.GetColumnType(this.PrimaryKey)} = ({queryString});") :
            new SqlPreCommandSimple($"DECLARE {variableName} {Connector.Current.SqlBuilder.GetColumnType(this.PrimaryKey)}; SET {variableName} = COALESCE(({queryString}), 1 / 0);");

            return result;
        }

        private SqlPreCommandSimple PostgresDoBlock(string variableName, SqlPreCommandSimple declaration, SqlPreCommand block)
        {
            return new SqlPreCommandSimple(@$"DO $$
DECLARE 
{declaration.PlainSql().Indent(4)}
BEGIN
    IF {variableName} IS NULL THEN 
        RAISE EXCEPTION 'Not found';
    END IF; 
{block.PlainSql().Indent(4)}
END $$;");
        }

        public event Func<Entity, SqlPreCommand?> PreDeleteSqlSync;

        SqlPreCommand? OnPreDeleteSqlSync(Entity entity)
        {
            if (PreDeleteSqlSync == null)
                return null;

            return PreDeleteSqlSync.GetInvocationListTyped().Select(a => a(entity)).Combine(Spacing.Simple);
        }
    }
}
