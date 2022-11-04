using Signum.Utilities.DataStructures;
using System.Collections.ObjectModel;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Linq;

/// <summary>
/// An extended expression comparer including custom DbExpression nodes
/// </summary>
internal class DbExpressionComparer : ExpressionComparer
{
    ScopedDictionary<Alias, Alias>? aliasMap;

    protected IDisposable AliasScope()
    {
        var saved = aliasMap;
        aliasMap = new ScopedDictionary<Alias, Alias>(aliasMap);
        return new Disposable(() => aliasMap = saved);
    }

    protected DbExpressionComparer(ScopedDictionary<ParameterExpression, ParameterExpression>? parameterScope, ScopedDictionary<Alias, Alias>? aliasScope, bool checkParameterNames)
        : base(parameterScope, checkParameterNames)
    {
        this.aliasMap = aliasScope;
    }

    public static bool AreEqual(Expression? a, Expression? b, ScopedDictionary<ParameterExpression, ParameterExpression>? parameterScope = null, ScopedDictionary<Alias, Alias>? aliasScope = null, bool checkParameterNames = false)
    {
        return new DbExpressionComparer(parameterScope, aliasScope, checkParameterNames ).Compare(a, b);
    }

    protected override bool Compare(Expression? a, Expression? b)
    {
        bool result = ComparePrivate(a, b);

        if (result == false)
            result = !!result; //Breakpoint here to check the first offender

        return result;
    }



    private bool ComparePrivate(Expression? a, Expression? b)
    {
        if (a == b)
            return true;
        if (a == null || b == null)
            return false;
        if (a.NodeType != b.NodeType)
            return false;
        if (a.Type != b.Type)
            return false;

        if (!(a is DbExpression))
            return base.Compare(a, b);

        if (((DbExpression)a).DbNodeType != ((DbExpression)b).DbNodeType)
            return false;

        return ((DbExpression)a).DbNodeType switch
        {
            DbExpressionType.Table => CompareTable((TableExpression)a, (TableExpression)b),
            DbExpressionType.Column => CompareColumn((ColumnExpression)a, (ColumnExpression)b),
            DbExpressionType.Select => CompareSelect((SelectExpression)a, (SelectExpression)b),
            DbExpressionType.Join => CompareJoin((JoinExpression)a, (JoinExpression)b),
            DbExpressionType.SetOperator => CompareSetOperator((SetOperatorExpression)a, (SetOperatorExpression)b),
            DbExpressionType.Projection => CompareProjection((ProjectionExpression)a, (ProjectionExpression)b),
            DbExpressionType.ChildProjection => CompareChildProjection((ChildProjectionExpression)a, (ChildProjectionExpression)b),
            DbExpressionType.Aggregate => CompareAggregate((AggregateExpression)a, (AggregateExpression)b),
            DbExpressionType.AggregateRequest => CompareAggregateSubquery((AggregateRequestsExpression)a, (AggregateRequestsExpression)b),
            DbExpressionType.SqlCast => CompareSqlCast((SqlCastExpression)a, (SqlCastExpression)b),
            DbExpressionType.SqlFunction => CompareSqlFunction((SqlFunctionExpression)a, (SqlFunctionExpression)b),
            DbExpressionType.SqlTableValuedFunction => CompareTableValuedSqlFunction((SqlTableValuedFunctionExpression)a, (SqlTableValuedFunctionExpression)b),
            DbExpressionType.SqlConstant => CompareSqlConstant((SqlConstantExpression)a, (SqlConstantExpression)b),
            DbExpressionType.SqlLiteral => CompareSqlLiteral((SqlLiteralExpression)a, (SqlLiteralExpression)b),
            DbExpressionType.Case => CompareCase((CaseExpression)a, (CaseExpression)b),
            DbExpressionType.RowNumber => CompareRowNumber((RowNumberExpression)a, (RowNumberExpression)b),
            DbExpressionType.Like => CompareLike((LikeExpression)a, (LikeExpression)b),
            DbExpressionType.Scalar or DbExpressionType.Exists or DbExpressionType.In => CompareSubquery((SubqueryExpression)a, (SubqueryExpression)b),
            DbExpressionType.IsNull => CompareIsNull((IsNullExpression)a, (IsNullExpression)b),
            DbExpressionType.IsNotNull => CompareIsNotNull((IsNotNullExpression)a, (IsNotNullExpression)b),
            DbExpressionType.Delete => CompareDelete((DeleteExpression)a, (DeleteExpression)b),
            DbExpressionType.Update => CompareUpdate((UpdateExpression)a, (UpdateExpression)b),
            DbExpressionType.InsertSelect => CompareInsertSelect((InsertSelectExpression)a, (InsertSelectExpression)b),
            DbExpressionType.CommandAggregate => CompareCommandAggregate((CommandAggregateExpression)a, (CommandAggregateExpression)b),
            DbExpressionType.Entity => CompareEntityInit((EntityExpression)a, (EntityExpression)b),
            DbExpressionType.EmbeddedInit => CompareEmbeddedFieldInit((EmbeddedEntityExpression)a, (EmbeddedEntityExpression)b),
            DbExpressionType.MixinInit => CompareMixinFieldInit((MixinEntityExpression)a, (MixinEntityExpression)b),
            DbExpressionType.ImplementedBy => CompareImplementedBy((ImplementedByExpression)a, (ImplementedByExpression)b),
            DbExpressionType.ImplementedByAll => CompareImplementedByAll((ImplementedByAllExpression)a, (ImplementedByAllExpression)b),
            DbExpressionType.LiteReference => CompareLiteReference((LiteReferenceExpression)a, (LiteReferenceExpression)b),
            DbExpressionType.LiteValue => CompareLiteValue((LiteValueExpression)a, (LiteValueExpression)b),
            DbExpressionType.TypeEntity => CompareTypeEntity((TypeEntityExpression)a, (TypeEntityExpression)b),
            DbExpressionType.TypeImplementedBy => CompareTypeImplementedBy((TypeImplementedByExpression)a, (TypeImplementedByExpression)b),
            DbExpressionType.TypeImplementedByAll => CompareTypeImplementedByAll((TypeImplementedByAllExpression)a, (TypeImplementedByAllExpression)b),
            DbExpressionType.MList => CompareMList((MListExpression)a, (MListExpression)b),
            DbExpressionType.MListElement => CompareMListElement((MListElementExpression)a, (MListElementExpression)b),
            DbExpressionType.PrimaryKey => ComparePrimaryKey((PrimaryKeyExpression)a, (PrimaryKeyExpression)b),
            DbExpressionType.AdditionalField => CompareAdditionalField((AdditionalFieldExpression)a, (AdditionalFieldExpression)b),
            _ => throw new InvalidOperationException("Unexpected " + ((DbExpression)a).DbNodeType),
        };
    }

    protected virtual bool CompareTable(TableExpression a, TableExpression b)
    {
        return object.Equals(a.Name, b.Name);
    }

    protected virtual bool CompareColumn(ColumnExpression a, ColumnExpression b)
    {
        return CompareAlias(a.Alias, b.Alias) && a.Name == b.Name;
    }

    protected virtual bool CompareAlias(Alias? a, Alias? b)
    {
        if (a == null && b == null)
            return true;

        if (a == null || b == null)
            return false;

        if (aliasMap != null)
        {
            if (aliasMap.TryGetValue(a, out Alias? mapped))
                return mapped == b;
        }
        return a == b;
    }

    protected virtual bool CompareSelect(SelectExpression a, SelectExpression b)
    {
        if (!Compare(a.From, b.From))
            return false;

        using (AliasScope())
        {
        MapAliases(a.From!, b.From!);

        return Compare(a.Where, b.Where)
            && CompareList(a.OrderBy, b.OrderBy, CompareOrder)
            && CompareList(a.GroupBy, b.GroupBy, Compare)
            && a.IsDistinct == b.IsDistinct
            && CompareColumnDeclarations(a.Columns, b.Columns);
    }
    }

    protected virtual void MapAliases(SourceExpression sourceA, SourceExpression sourceB)
    {
        for (int i = 0, n = sourceA.KnownAliases.Length; i < n; i++)
        {
            aliasMap!.Add(sourceA.KnownAliases[i], sourceB.KnownAliases[i]);
        }
    }

    protected virtual bool CompareOrder(OrderExpression a, OrderExpression b)
    {
        return a.OrderType == b.OrderType && Compare(a.Expression, b.Expression);
    }

    protected virtual bool CompareColumnDeclarations(ReadOnlyCollection<ColumnDeclaration> a, ReadOnlyCollection<ColumnDeclaration> b)
    {
        if (a == b)
            return true;
        if (a == null || b == null)
            return false;
        if (a.Count != b.Count)
            return false;
        for (int i = 0, n = a.Count; i < n; i++)
        {
            if (!CompareColumnDeclaration(a[i], b[i]))
                return false;
        }
        return true;
    }

    protected virtual bool CompareColumnDeclaration(ColumnDeclaration a, ColumnDeclaration b)
    {
        return a.Name == b.Name && Compare(a.Expression, b.Expression);
    }

    protected virtual bool CompareJoin(JoinExpression a, JoinExpression b)
    {
        if (a.JoinType != b.JoinType)
            return false;

            if (!Compare(a.Left, b.Left))
                return false;

        if (a.JoinType == JoinType.CrossApply || a.JoinType == JoinType.OuterApply)
        {
            using (AliasScope())
            {
                MapAliases(a.Left, b.Left);

                return Compare(a.Right, b.Right)
                    && Compare(a.Condition, b.Condition);
            }
        }
        else
        {
            if (!Compare(a.Left, b.Left))
                return false;

            if (!Compare(a.Right, b.Right))
                return false;

            using (AliasScope())
            {
                MapAliases(a.Left, b.Left);
                MapAliases(a.Right, b.Right);

                return Compare(a.Condition, b.Condition);
            }
        }
    }

    protected virtual bool CompareSetOperator(SetOperatorExpression a, SetOperatorExpression b)
    {
        if (a.Operator != b.Operator)
            return false;

        if (!CompareAlias(a.Alias, b.Alias))
            return false;

        if (!Compare(a.Left, b.Left))
            return false;

        if (!Compare(a.Right, b.Right))
            return false;

        return true;
    }


    protected virtual bool CompareProjection(ProjectionExpression a, ProjectionExpression b)
    {
        if (a.UniqueFunction != b.UniqueFunction)
            return false;

        if (!Compare(a.Select, b.Select))
            return false;

        using (AliasScope())
        {
            MapAliases(a.Select, b.Select);

            return Compare(a.Projector, b.Projector);
        }
    }

    private bool CompareChildProjection(ChildProjectionExpression a, ChildProjectionExpression b)
    {
        return Compare(a.Projection, b.Projection)
            && Compare(a.OuterKey, b.OuterKey)
            && a.IsLazyMList == b.IsLazyMList;
    }

    protected virtual bool CompareAggregate(AggregateExpression a, AggregateExpression b)
    {
        return a.AggregateFunction == b.AggregateFunction && CompareList(a.Arguments, b.Arguments, Compare);
    }

    protected virtual bool CompareAggregateSubquery(AggregateRequestsExpression a, AggregateRequestsExpression b)
    {
        return Compare(a.Aggregate, b.Aggregate)
            && a.GroupByAlias == b.GroupByAlias;
    }

    protected virtual bool CompareSqlCast(SqlCastExpression a, SqlCastExpression b)
    {
        return a.DbType.Equals(b.DbType)
            && Compare(a.Expression, b.Expression);
    }

    protected virtual bool CompareSqlFunction(SqlFunctionExpression a, SqlFunctionExpression b)
    {
        return a.SqlFunction == b.SqlFunction
            && Compare(a.Object, b.Object)
            && CompareList(a.Arguments, b.Arguments, Compare);
    }


    private bool CompareTableValuedSqlFunction(SqlTableValuedFunctionExpression a, SqlTableValuedFunctionExpression b)
    {
        return a.ViewTable == b.ViewTable
          && CompareAlias(a.Alias, b.Alias)
          && CompareList(a.Arguments, b.Arguments, Compare);
    }

    protected virtual bool CompareSqlConstant(SqlConstantExpression a, SqlConstantExpression b)
    {
        return object.Equals(a.Value, b.Value);
    }

    protected virtual bool CompareSqlLiteral(SqlLiteralExpression a, SqlLiteralExpression b)
    {
        return a.Value == b.Value;
    }


    protected virtual bool CompareCase(CaseExpression a, CaseExpression b)
    {
        return CompareList(a.Whens, b.Whens, CompareWhen)
            && Compare(a.DefaultValue, b.DefaultValue);
    }

    protected virtual bool CompareWhen(When a, When b)
    {
        return Compare(a.Condition, b.Condition)
            && Compare(a.Value, b.Value);
    }

    protected virtual bool CompareRowNumber(RowNumberExpression a, RowNumberExpression b)
    {
        return CompareList(a.OrderBy, b.OrderBy, CompareOrder);
    }

    protected virtual bool CompareLike(LikeExpression a, LikeExpression b)
    {
        return Compare(a.Expression, b.Expression)
            && Compare(a.Pattern, b.Pattern);
    }

    protected virtual bool CompareSubquery(SubqueryExpression a, SubqueryExpression b)
    {
        if (a.NodeType != b.NodeType)
            return false;
        
        return a.DbNodeType switch
        {
            DbExpressionType.Scalar => CompareScalar((ScalarExpression)a, (ScalarExpression)b),
            DbExpressionType.Exists => CompareExists((ExistsExpression)a, (ExistsExpression)b),
            DbExpressionType.In => CompareIn((InExpression)a, (InExpression)b),
            _ => false,
        };
    }

    protected virtual bool CompareScalar(ScalarExpression a, ScalarExpression b)
    {
            return Compare(a.Select, b.Select);
    }

    protected virtual bool CompareExists(ExistsExpression a, ExistsExpression b)
    {
            return Compare(a.Select, b.Select);
    }

    protected virtual bool CompareIn(InExpression a, InExpression b)
    {
        return Compare(a.Expression, b.Expression)
            && Compare(a.Select, b.Select)
            && CompareValues(a.Values, b.Values);
    }

    protected virtual bool CompareValues(object[]? a, object[]? b)
    {
        if (a == b)
            return true;

        if (a == null || b == null)
            return false;

        return a.SequenceEqual(b);
    }

    protected virtual bool CompareIsNull(IsNullExpression a, IsNullExpression b)
    {
        return Compare(a.Expression, b.Expression);
    }

    protected virtual bool CompareIsNotNull(IsNotNullExpression a, IsNotNullExpression b)
    {
        return Compare(a.Expression, b.Expression);
    }

    protected virtual bool CompareDelete(DeleteExpression a, DeleteExpression b)
    {
        return a.Table == b.Table
            && a.UseHistoryTable == b.UseHistoryTable
            && Compare(a.Source, b.Source)
            && Compare(a.Where, b.Where)
            && a.ReturnRowCount == b.ReturnRowCount;
    }

    protected virtual bool CompareUpdate(UpdateExpression a, UpdateExpression b)
    {
        return a.Table == b.Table
            && a.UseHistoryTable == b.UseHistoryTable
            && CompareList(a.Assigments, b.Assigments, CompareAssigment)
            && Compare(a.Source, b.Source)
            && Compare(a.Where, b.Where)
            && a.ReturnRowCount == b.ReturnRowCount;
    }

    protected virtual bool CompareInsertSelect(InsertSelectExpression a, InsertSelectExpression b)
    {
        return a.Table == b.Table
            && a.UseHistoryTable == b.UseHistoryTable
            && CompareList(a.Assigments, b.Assigments, CompareAssigment)
            && Compare(a.Source, b.Source)
            && a.ReturnRowCount == b.ReturnRowCount;
    }

    protected virtual bool CompareAssigment(ColumnAssignment a, ColumnAssignment b)
    {
        return a.Column == b.Column && Compare(a.Expression, b.Expression);
    }

    protected virtual bool CompareCommandAggregate(CommandAggregateExpression a, CommandAggregateExpression b)
    {
        return CompareList(a.Commands, b.Commands, Compare);
    }

    protected virtual bool CompareEntityInit(EntityExpression a, EntityExpression b)
    {
        return a.Table == b.Table
            && CompareAlias(a.TableAlias, b.TableAlias)
            && Compare(a.ExternalId, b.ExternalId)
            && CompareList(a.Bindings, b.Bindings, CompareFieldBinding)
            && CompareList(a.Mixins, b.Mixins, CompareMixinFieldInit);
    }

    protected virtual bool CompareEmbeddedFieldInit(EmbeddedEntityExpression a, EmbeddedEntityExpression b)
    {
        return Compare(a.HasValue, b.HasValue)
            && a.FieldEmbedded == b.FieldEmbedded
            && CompareList(a.Bindings, b.Bindings, CompareFieldBinding);
    }

    protected virtual bool CompareMixinFieldInit(MixinEntityExpression a, MixinEntityExpression b)
    {
        return a.FieldMixin == b.FieldMixin
            && CompareList(a.Bindings, b.Bindings, CompareFieldBinding);
    }

    protected virtual bool CompareFieldBinding(FieldBinding a, FieldBinding b)
    {
        return ReflectionTools.FieldEquals(a.FieldInfo, b.FieldInfo) && Compare(a.Binding, b.Binding);
    }

    protected virtual bool CompareImplementedBy(ImplementedByExpression a, ImplementedByExpression b)
    {
        return CompareDictionaries(a.Implementations, b.Implementations, Compare);
    }

    protected virtual bool CompareImplementedByAll(ImplementedByAllExpression a, ImplementedByAllExpression b)
    {
        return Compare(a.TypeId, b.TypeId)
            && CompareDictionaries<Type, Expression>(a.Ids, b.Ids, Compare);
    }

    protected virtual bool CompareLiteReference(LiteReferenceExpression a, LiteReferenceExpression b)
    {
        return Compare(a.Reference, b.Reference)
            && Compare(a.CustomModelExpression, b.CustomModelExpression)
            && CompareDictionaries(a.CustomModelTypes, b.CustomModelTypes, (at, bt) => at == bt);
    }

    protected virtual bool CompareLiteValue(LiteValueExpression a, LiteValueExpression b)
    {
        return Compare(a.Id, b.Id)
           && Compare(a.TypeId, b.TypeId)
           && Compare(a.CustomModelExpression, b.CustomModelExpression)
           && CompareDictionaries(a.Models, b.Models, (a1, b1) => a1.LazyModelType == b1.LazyModelType && Compare(a1.EagerExpression, b1.EagerExpression));
    }

    protected virtual bool CompareTypeEntity(TypeEntityExpression a, TypeEntityExpression b)
    {
        return a.TypeValue == b.TypeValue
           && Compare(a.ExternalId, b.ExternalId);
    }

    protected virtual bool CompareTypeImplementedBy(TypeImplementedByExpression a, TypeImplementedByExpression b)
    {
        return CompareDictionaries(a.TypeImplementations, b.TypeImplementations, Compare);
    }

    protected virtual bool CompareTypeImplementedByAll(TypeImplementedByAllExpression a, TypeImplementedByAllExpression b)
    {
        return Compare(a.TypeColumn, b.TypeColumn);
    }

    protected virtual bool CompareMList(MListExpression a, MListExpression b)
    {
        return a.TableMList == b.TableMList
            && Compare(a.BackID, b.BackID);
    }

    protected virtual bool CompareAdditionalField(AdditionalFieldExpression a, AdditionalFieldExpression b)
    {
        return a.Route.Equals(b.Route)
            && Compare(a.BackID, b.BackID);
    }

    protected virtual bool CompareMListElement(MListElementExpression a, MListElementExpression b)
    {
        return a.Table == b.Table
            && Compare(a.RowId, b.RowId)
            && Compare(a.Element, b.Element)
            && Compare(a.Order, b.Order)
            && Compare(a.Parent, b.Parent);
    }

    protected virtual bool ComparePrimaryKey(PrimaryKeyExpression a, PrimaryKeyExpression b)
    {
        return Compare(a.Value, b.Value);
    }

    public static new IEqualityComparer<E> GetComparer<E>(bool checkParameterNames) where E : Expression
    {
        return new DbExpressionsEqualityComparer<E>(checkParameterNames);
    }

    class DbExpressionsEqualityComparer<E> : IEqualityComparer<E> where E : Expression
    {
        bool checkParameterNames;
        public DbExpressionsEqualityComparer(bool checkParameterNames)
        {
            this.checkParameterNames = checkParameterNames;
        }

        public bool Equals(E? x, E? y)
        {
            return DbExpressionComparer.AreEqual(x, y, checkParameterNames: this.checkParameterNames);
        }

        public int GetHashCode(E obj)
        {
            return obj.Type.GetHashCode() ^ obj.NodeType.GetHashCode() ^ SpacialHash(obj);
        }

        private static int SpacialHash(Expression obj)
        {
            if (obj is MethodCallExpression mce)
                return mce.Method.Name.GetHashCode();

            if (obj is MemberExpression me)
                return me.Member.Name.GetHashCode();

            return 0;
        }
    }
}
