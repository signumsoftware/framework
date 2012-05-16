using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Engine.Maps;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Linq
{
    internal class BinderTools
    {
        Dictionary<ProjectionToken, HashSet<TableCondition>> requests = new Dictionary<ProjectionToken, HashSet<TableCondition>>();

        public BinderTools()
        {
        }

        public void AddRequest(ProjectionToken projectionToken, TableCondition tableCondition)
        {
            requests.GetOrCreate(projectionToken).Add(tableCondition);
        }

        public ProjectionExpression ApplyExpansions(ProjectionExpression projection)
        {
            if (!requests.ContainsKey(projection.Token))
                return projection;

            HashSet<TableCondition> allProjections = requests.Extract(projection.Token);

            Alias newAlias = Alias.NextSelectAlias();
            Alias[] oldAliases = allProjections.Select(p => p.Table.Alias).And(projection.Source.Alias).ToArray();

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, newAlias, oldAliases, new ProjectionToken[0]); //Do not replace tokens

            JoinExpression source = (JoinExpression)allProjections.Aggregate((SourceExpression)projection.Source, (e, p) =>
            {
                var externalID = DbExpressionNominator.FullNominate(p.FieldInit.ExternalId);

                Expression equal = SmartEqualizer.EqualNullable(externalID, p.FieldInit.GetFieldBinding(FieldInitExpression.IdField));
                return new JoinExpression(JoinType.SingleRowLeftOuterJoin, e, p.Table, equal);
            });

            return new ProjectionExpression(
                    new SelectExpression(newAlias, false, false, null, pc.Columns, source, null, null, null),
                    pc.Projector, projection.UniqueFunction, projection.Token, projection.Type);
        }

        internal static SqlConstantExpression NullId = new SqlConstantExpression(null, typeof(int?));

        public Expression MakeLite(Type type, Expression entity, Expression toStr)
        {
            if (toStr == null && !(entity is ImplementedByAllExpression))
                toStr = GetToStr(entity);

            Expression id = GetId(entity);
            Expression typeId = GetEntityType(entity);
            return new LiteReferenceExpression(type, entity, id, toStr, typeId);
        }

        private Expression GetToStr(Expression expression)
        {
            if (expression is FieldInitExpression)
            {
                FieldInitExpression fie = (FieldInitExpression)expression;

                return fie.GetOrCreateFieldBinding(FieldInitExpression.ToStrField, this);
            }

            if (expression is ImplementedByExpression)
            {
                ImplementedByExpression ib = (ImplementedByExpression)expression;

                if (ib.Implementations.Count == 0)
                    return new SqlConstantExpression(null, typeof(string));

                if (ib.Implementations.Count == 1)
                    return ib.Implementations[0].Field.GetOrCreateFieldBinding(FieldInitExpression.ToStrField, this);//Not regular, but usefull
                return ib.Implementations.Select(imp =>new When(
                    Expression.NotEqual(imp.Field.ExternalId, NullId),
                    imp.Field.GetOrCreateFieldBinding(FieldInitExpression.ToStrField, this)))
                    .ToCondition(typeof(string));
            }

            if (expression is ImplementedByAllExpression)
            {
                return null;
            }

            if (expression.NodeType == ExpressionType.Conditional)
            {
                var con = (ConditionalExpression)expression;
                return Condition(con.Test, GetToStr(con.IfTrue), GetToStr(con.IfFalse));
            }

            if (expression.NodeType == ExpressionType.Coalesce)
            {
                var bin = (BinaryExpression)expression;
                return Condition(Expression.NotEqual(GetId(bin.Left).Nullify(), NullId),
                    GetToStr(bin.Left), GetToStr(bin.Right));
            }

            throw new NotSupportedException();
        }

        public Expression GetId(Expression expression)
        {
            if (expression is FieldInitExpression)
                return ((FieldInitExpression)expression).ExternalId;

            if (expression is ImplementedByExpression)
            {
                ImplementedByExpression ib = (ImplementedByExpression)expression;

                Expression aggregate = Coalesce(typeof(int?), ib.Implementations.Select(imp => imp.Field.ExternalId));

                return aggregate;
            }
            if (expression is ImplementedByAllExpression)
                return ((ImplementedByAllExpression)expression).Id;

            if (expression.NodeType == ExpressionType.Conditional)
            {
                var con = (ConditionalExpression)expression;
                return Condition(con.Test, GetId(con.IfTrue), GetId(con.IfFalse));
            }

            if (expression.NodeType == ExpressionType.Coalesce)
            {
                var bin = (BinaryExpression)expression;

                return Condition(Expression.NotEqual(GetId(bin.Left).Nullify(), NullId), GetId(bin.Left), GetId(bin.Right));
            }

            throw new NotSupportedException("Id for {0}".Formato(expression.NiceToString())); 
        }

        public Expression GetEntityType(Expression expression)
        {
            if (expression is FieldInitExpression)
            {
                FieldInitExpression fie = (FieldInitExpression)expression;

                return new TypeFieldInitExpression(fie.ExternalId, fie.Type);
            }

            if (expression is ImplementedByExpression)
            {
                ImplementedByExpression ib = (ImplementedByExpression)expression;

                if (ib.Implementations.Count == 0)
                    return Expression.Constant(null, typeof(Type));

                return new TypeImplementedByExpression(ib.Implementations.Select(imp => new TypeImplementationColumnExpression(imp.Type, imp.Field.ExternalId)).ToReadOnly()); 
            }

            if (expression is ImplementedByAllExpression)
            {
                return ((ImplementedByAllExpression)expression).TypeId;
            }

            if (expression.NodeType == ExpressionType.Conditional)
            {
                var con = (ConditionalExpression)expression;
                return Condition(con.Test, GetEntityType(con.IfTrue), GetEntityType(con.IfFalse));
            }

            if (expression.NodeType == ExpressionType.Coalesce)
            {
                var bin = (BinaryExpression)expression;
                return Condition(Expression.NotEqual(GetId(bin.Left).Nullify(), NullId),
                    GetEntityType(bin.Left), GetEntityType(bin.Right));
            }

            return null;
        }

        private static Expression Condition(Expression test, Expression ifTrue, Expression ifFalse)
        {
            if (ifTrue.Type.IsNullable() || ifFalse.Type.IsNullable())
            {
                ifTrue = ifTrue.Nullify();
                ifFalse = ifFalse.Nullify();
            }
            return Expression.Condition(test, ifTrue, ifFalse);
        }

        public Expression Coalesce(Type type, IEnumerable<Expression> exp)
        {
            var list = exp.ToList();

            if (list.IsEmpty())
                return Expression.Constant(null, type);

            if (list.Count() == 1)
                return list[0]; //Not regular, but usefull

            return exp.Reverse().Aggregate((ac, e) => Expression.Coalesce(e, ac));
        }

        internal ProjectionExpression MListProjection(MListExpression mle)
        {
            RelationalTable tr = mle.RelationalTable;

            Alias tableAlias = NextSelectAlias();
            TableExpression tableExpression = new TableExpression(tableAlias, tr.Name);

            ProjectionToken token = new ProjectionToken();

            Expression expr = tr.FieldExpression(token, tableAlias, this);

            Alias selectAlias = NextSelectAlias();

            ColumnExpression ce = tr.BackColumnExpression(tableAlias);

            ProjectedColumns pc = ColumnProjector.ProjectColumns(expr, selectAlias, tableExpression.KnownAliases, new ProjectionToken[0]); // no Token

            var proj = new ProjectionExpression(
                new SelectExpression(selectAlias, false, false, null, pc.Columns, tableExpression, SmartEqualizer.EqualNullable(mle.BackID, ce), null, null),
                 pc.Projector, null, token, mle.Type);

            proj = ApplyExpansions(proj);

            return proj;
        }

        internal Alias NextSelectAlias()
        {
            return Alias.NextSelectAlias();
        }

        internal Alias NextTableAlias(string tableName)
        {
            return Alias.NextTableAlias(tableName);
        }
    }

    internal class TableCondition
    {
        public TableExpression Table;
        public FieldInitExpression FieldInit;
    }
}
