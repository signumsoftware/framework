using Signum.Engine.Linq;
using Signum.Engine.Sync;
using Signum.Utilities.Reflection;
using System.Data;

namespace Signum.Engine.Maps;

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
        var pre = Schema.Current.EntityEvents<T>().OnPreDeleteSqlSync(entity);
        var collections = (from tml in this.TablesMList()
                           select new SqlPreCommandSimple("DELETE FROM {0} WHERE {1} = {2}; --{3}"
                               .FormatWith(tml.Name, tml.BackReference.Name.SqlEscape(isPostgres), variableOrId, comment ?? entity.ToString()))).Combine(Spacing.Simple);

        var vmis = VirtualMList.RegisteredVirtualMLists.TryGetC(this.Type);

        SqlPreCommand? virtualMList = vmis?.Values
            .Select(vmi => giDeleteVirtualMListSync.GetInvoker(this.Type, vmi.BackReferenceRoute.RootType)(entity, vmi))
            .Combine(Spacing.Double);

        var main = new SqlPreCommandSimple("DELETE FROM {0} WHERE {1} = {2}; --{3}"
                .FormatWith(Name, this.PrimaryKey.Name.SqlEscape(isPostgres), variableOrId, comment ?? entity.ToString()));


        if(declaration != null)
        {
            SqlPreCommandSimple ifStatement = AssertNotNull(entity.Id.VariableName!, this.Name.Name, isPostgres);
            if (isPostgres)
            {
                var block = new SqlPreCommandPostgresDoBlock(
                    declarations: [declaration],
                    body: SqlPreCommand.Combine(Spacing.Simple, ifStatement, pre, collections, virtualMList, main)!
                    );

                return block.SimplifyNested();
            }
            else
            {

                return SqlPreCommand.Combine(Spacing.Simple, declaration, ifStatement, pre, collections, virtualMList, main)!;
            }
        }

        return SqlPreCommand.Combine(Spacing.Simple, pre, collections, virtualMList, main)!;
    }

    private static SqlPreCommandSimple AssertNotNull(string variableName, string typeName, bool isPostgres)
    {
        if (isPostgres)
        {
            return new SqlPreCommandSimple(@$"IF {variableName} IS NULL THEN 
    RAISE EXCEPTION '{typeName} not found';
END IF;");
        }else
        {
            return new SqlPreCommandSimple(@$"IF({variableName} IS NULL)
BEGIN
	RAISERROR('{typeName} not found!', 11, 1);
	RETURN;
END;");
        }

    }

    static GenericInvoker<Func<Entity, VirtualMListInfo, SqlPreCommand?>> giDeleteVirtualMListSync = new GenericInvoker<Func<Entity, VirtualMListInfo, SqlPreCommand?>>(
      (e, vmi) => DeleteVirtualMListSync<Entity, Entity>(e, vmi));
    static SqlPreCommand? DeleteVirtualMListSync<T, E>(T entity, VirtualMListInfo vmli)
        where T : Entity
        where E : Entity
    {
        var table = Schema.Current.Table(typeof(E));

        var backRef = vmli.BackReferenceRoute.GetLambdaExpression<E, Lite<T>>(safeNullAccess: false);

        var delete = Administrator.UnsafeDeletePreCommand(Database.Query<E>().Where(e => backRef.Evaluate(e).Is(entity)));

        return delete;
    }

    int parameterIndex;
    private SqlPreCommandSimple DeclarePrimaryKeyVariable<T>(T entity, Expression<Func<T, bool>> where) where T : Entity
    {
        var query = Database.Query<T>().Where(where);

        var uniqueFilters = Schema.Current.AttachToUniqueFilter?.GetInvocationListTyped().Select(f => f.Invoke(entity)).NotNull().ToList();
        if (uniqueFilters != null && uniqueFilters.Any())
            uniqueFilters.ForEach(f => query = query.Where(e => f.Evaluate(e)));

        var queryCommand = DbQueryProvider.Single.GetMainSqlCommand(query.Select(a => a.Id).Expression);

        string variableName = this.Name.Name + "Id_" + (parameterIndex++);
        if (!Schema.Current.Settings.IsPostgres)
            variableName = SqlParameterBuilder.GetParameterName(variableName);

        entity.SetId(new Entities.PrimaryKey(entity.id!.Value.Object, variableName));

        string queryCommandString = queryCommand.PlainSql().Lines().ToString(" ");

        var columnType = Connector.Current.SqlBuilder.GetColumnType(this.PrimaryKey);

        var result = Schema.Current.Settings.IsPostgres ?
        new SqlPreCommandSimple(@$"{variableName} {columnType} = ({queryCommandString});") :
        new SqlPreCommandSimple($"DECLARE {variableName} {columnType} = ({queryCommandString});");

        return result;
    }

}
