using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.DataStructures;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using Signum.Utilities.Reflection;
using Signum.Utilities;

namespace Signum.Engine.Linq
{    /// <summary>
    /// An extended expression comparer including custom DbExpression nodes
    /// </summary>
    internal class DbExpressionComparer : ExpressionComparer
    {
        ScopedDictionary<Alias, Alias> aliasMap;

        protected IDisposable AliasScope()
        {
            var saved = aliasMap;
            aliasMap = new ScopedDictionary<Alias, Alias>(aliasMap);
            return new Disposable(() => aliasMap = saved);
        }

        protected DbExpressionComparer(ScopedDictionary<ParameterExpression, ParameterExpression> parameterScope, ScopedDictionary<Alias, Alias> aliasScope, bool checkParameterNames)
            : base(parameterScope, checkParameterNames)
        {
            this.aliasMap = aliasScope;
        }

        public static bool AreEqual(Expression a, Expression b, ScopedDictionary<ParameterExpression, ParameterExpression> parameterScope = null, ScopedDictionary<Alias, Alias> aliasScope = null, bool checkParameterNames = false)
        {
            return new DbExpressionComparer(parameterScope, aliasScope, checkParameterNames ).Compare(a, b);
        }

        protected override bool Compare(Expression a, Expression b)
        {
            bool result = ComparePrivate(a, b);

            if (result == false)
                result = !!result;

            return result;
        }



        private bool ComparePrivate(Expression a, Expression b)
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

            switch (((DbExpression)a).DbNodeType)
            {
                case DbExpressionType.Table:
                    return CompareTable((TableExpression)a, (TableExpression)b);
                case DbExpressionType.Column:
                    return CompareColumn((ColumnExpression)a, (ColumnExpression)b);
                case DbExpressionType.Select:
                    return CompareSelect((SelectExpression)a, (SelectExpression)b);
                case DbExpressionType.Join:
                    return CompareJoin((JoinExpression)a, (JoinExpression)b);
                case DbExpressionType.SetOperator:
                    return CompareSetOperator((SetOperatorExpression)a, (SetOperatorExpression)b);
                case DbExpressionType.Projection:
                    return CompareProjection((ProjectionExpression)a, (ProjectionExpression)b);
                case DbExpressionType.ChildProjection:
                    return CompareChildProjection((ChildProjectionExpression)a, (ChildProjectionExpression)b);
                case DbExpressionType.Aggregate:
                    return CompareAggregate((AggregateExpression)a, (AggregateExpression)b);
                case DbExpressionType.AggregateRequest:
                    return CompareAggregateSubquery((AggregateRequestsExpression)a, (AggregateRequestsExpression)b);
                case DbExpressionType.SqlCast:
                    return CompareSqlCast((SqlCastExpression)a, (SqlCastExpression)b);
                case DbExpressionType.SqlFunction:
                    return CompareSqlFunction((SqlFunctionExpression)a, (SqlFunctionExpression)b);
                case DbExpressionType.SqlTableValuedFunction:
                    return CompareTableValuedSqlFunction((SqlTableValuedFunctionExpression)a, (SqlTableValuedFunctionExpression)b);
                case DbExpressionType.SqlConstant:
                    return CompareSqlConstant((SqlConstantExpression)a, (SqlConstantExpression)b);
                case DbExpressionType.Case:
                    return CompareCase((CaseExpression)a, (CaseExpression)b);
                case DbExpressionType.RowNumber:
                    return CompareRowNumber((RowNumberExpression)a, (RowNumberExpression)b);
                case DbExpressionType.Like:
                    return CompareLike((LikeExpression)a, (LikeExpression)b);
                case DbExpressionType.Scalar:
                case DbExpressionType.Exists:
                case DbExpressionType.In:
                    return CompareSubquery((SubqueryExpression)a, (SubqueryExpression)b);
                case DbExpressionType.IsNull:
                    return CompareIsNull((IsNullExpression)a, (IsNullExpression)b);
                case DbExpressionType.IsNotNull:
                    return CompareIsNotNull((IsNotNullExpression)a, (IsNotNullExpression)b);
                case DbExpressionType.Delete:
                    return CompareDelete((DeleteExpression)a, (DeleteExpression)b);
                case DbExpressionType.Update:
                    return CompareUpdate((UpdateExpression)a, (UpdateExpression)b);
                case DbExpressionType.InsertSelect:
                    return CompareInsertSelect((InsertSelectExpression)a, (InsertSelectExpression)b);
                case DbExpressionType.CommandAggregate:
                    return CompareCommandAggregate((CommandAggregateExpression)a, (CommandAggregateExpression)b);
                case DbExpressionType.SelectRowCount:
                    return CompareSelectRowCount((SelectRowCountExpression)a, (SelectRowCountExpression)b);
                case DbExpressionType.Entity:
                    return CompareEntityInit((EntityExpression)a, (EntityExpression)b);
                case DbExpressionType.EmbeddedInit:
                    return CompareEmbeddedFieldInit((EmbeddedEntityExpression)a, (EmbeddedEntityExpression)b);
                case DbExpressionType.MixinInit:
                    return CompareMixinFieldInit((MixinEntityExpression)a, (MixinEntityExpression)b);
                case DbExpressionType.ImplementedBy:
                    return CompareImplementedBy((ImplementedByExpression)a, (ImplementedByExpression)b);
                case DbExpressionType.ImplementedByAll:
                    return CompareImplementedByAll((ImplementedByAllExpression)a, (ImplementedByAllExpression)b);
                case DbExpressionType.LiteReference:
                    return CompareLiteReference((LiteReferenceExpression)a, (LiteReferenceExpression)b);
                case DbExpressionType.LiteValue:
                    return CompareLiteValue((LiteValueExpression)a, (LiteValueExpression)b);
                case DbExpressionType.TypeEntity:
                    return CompareTypeFieldInit((TypeEntityExpression)a, (TypeEntityExpression)b);
                case DbExpressionType.TypeImplementedBy:
                    return CompareTypeImplementedBy((TypeImplementedByExpression)a, (TypeImplementedByExpression)b);
                case DbExpressionType.TypeImplementedByAll:
                    return CompareTypeImplementedByAll((TypeImplementedByAllExpression)a, (TypeImplementedByAllExpression)b);
                case DbExpressionType.MList:
                    return CompareMList((MListExpression)a, (MListExpression)b);
                case DbExpressionType.MListElement:
                    return CompareMListElement((MListElementExpression)a, (MListElementExpression)b);
                case DbExpressionType.PrimaryKey:
                    return ComparePrimaryKey((PrimaryKeyExpression)a, (PrimaryKeyExpression)b);
                default:
                    throw new InvalidOperationException("Unexpected " + ((DbExpression)a).DbNodeType);
                 
            }
        }


        protected virtual bool CompareTable(TableExpression a, TableExpression b)
        {
            return object.Equals(a.Name, b.Name);
        }

        protected virtual bool CompareColumn(ColumnExpression a, ColumnExpression b)
        {
            return CompareAlias(a.Alias, b.Alias) && a.Name == b.Name;
        }

        protected virtual bool CompareAlias(Alias a, Alias b)
        {
            if (a == null && b == null)
                return true;

            if (a == null || b == null)
                return false;

            if (aliasMap != null)
            {
                if (aliasMap.TryGetValue(a, out Alias mapped))
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
                MapAliases(a.From, b.From);

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
                aliasMap.Add(sourceA.KnownAliases[i], sourceB.KnownAliases[i]);
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
                return Compare(a.Right, b.Right)
                    && Compare(a.Condition, b.Condition);
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
            return a.AggregateFunction == b.AggregateFunction && Compare(a.Expression, b.Expression);
        }

        protected virtual bool CompareAggregateSubquery(AggregateRequestsExpression a, AggregateRequestsExpression b)
        {
            return Compare(a.Aggregate, b.Aggregate)
                && a.GroupByAlias == b.GroupByAlias;
        }

        protected virtual bool CompareSqlCast(SqlCastExpression a, SqlCastExpression b)
        {
            return a.SqlDbType == b.SqlDbType
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
            return a.Table == b.Table
              && CompareAlias(a.Alias, b.Alias)
              && CompareList(a.Arguments, b.Arguments, Compare);
        }

        protected virtual bool CompareSqlConstant(SqlConstantExpression a, SqlConstantExpression b)
        {
            return object.Equals(a.Value, b.Value);
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
            switch ((DbExpressionType)a.NodeType)
            {
                case DbExpressionType.Scalar:
                    return CompareScalar((ScalarExpression)a, (ScalarExpression)b);
                case DbExpressionType.Exists:
                    return CompareExists((ExistsExpression)a, (ExistsExpression)b);
                case DbExpressionType.In:
                    return CompareIn((InExpression)a, (InExpression)b);
            }
            return false;
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
                && CompareValues(a.Values , b.Values); 
        }

        protected virtual bool CompareValues(object[] a, object[] b)
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
                && Compare(a.Where, b.Where);
        }

        protected virtual bool CompareUpdate(UpdateExpression a, UpdateExpression b)
        {
            return a.Table == b.Table
                && a.UseHistoryTable == b.UseHistoryTable
                && CompareList(a.Assigments, b.Assigments, CompareAssigment)
                && Compare(a.Source, b.Source)
                && Compare(a.Where, b.Where);
        }

        protected virtual bool CompareInsertSelect(InsertSelectExpression a, InsertSelectExpression b)
        {
            return a.Table == b.Table
                && a.UseHistoryTable == b.UseHistoryTable
                && CompareList(a.Assigments, b.Assigments, CompareAssigment)
                && Compare(a.Source, b.Source);
        }

        protected virtual bool CompareAssigment(ColumnAssignment a, ColumnAssignment b)
        {
            return a.Column == b.Column && Compare(a.Expression, b.Expression);
        }

        protected virtual bool CompareCommandAggregate(CommandAggregateExpression a, CommandAggregateExpression b)
        {
            return CompareList(a.Commands, b.Commands, Compare);
        }

        protected virtual bool CompareSelectRowCount(SelectRowCountExpression a, SelectRowCountExpression b)
        {
            return true;
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
                && Compare(a.Id, b.Id);
        }

        protected virtual bool CompareLiteReference(LiteReferenceExpression a, LiteReferenceExpression b)
        {
            return Compare(a.Reference, b.Reference) && Compare(a.CustomToStr, b.CustomToStr);
        }

        protected virtual bool CompareLiteValue(LiteValueExpression a, LiteValueExpression b)
        {
            return Compare(a.Id, b.Id)
               && Compare(a.ToStr, b.ToStr)
               && Compare(a.TypeId, b.TypeId);
        }

        protected virtual bool CompareTypeFieldInit(TypeEntityExpression a, TypeEntityExpression b)
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

        protected virtual bool CompareMList(AdditionalFieldExpression a, AdditionalFieldExpression b)
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

            public bool Equals(E x, E y)
            {
                return DbExpressionComparer.AreEqual(x, y, checkParameterNames: this.checkParameterNames);
            }

            public int GetHashCode(E obj)
            {
                return obj.Type.GetHashCode() ^ obj.NodeType.GetHashCode() ^ SpacialHash(obj);
            }

            private int SpacialHash(Expression obj)
            {
                if (obj is MethodCallExpression)
                    return ((MethodCallExpression)obj).Method.Name.GetHashCode();

                if (obj is MemberExpression)
                    return ((MemberExpression)obj).Member.Name.GetHashCode();

                return 0;
            }
        }
    }
}
