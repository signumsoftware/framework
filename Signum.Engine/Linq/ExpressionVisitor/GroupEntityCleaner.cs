using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace Signum.Engine.Linq
{
    internal class GroupEntityCleaner : DbExpressionVisitor
    {
        public static Expression Clean(Expression source)
        {
            GroupEntityCleaner pc = new GroupEntityCleaner();
            return pc.Visit(source);
        }

        [return: NotNullIfNotNull("exp")]
        public override Expression? Visit(Expression? exp)
        {
            if (exp == null)
                return null!;

            if (exp.Type == typeof(Type))
                return VisitType(exp);
            else
                return base.Visit(exp);
        }

        private Expression VisitType(Expression exp)
        {
            if (exp.NodeType == ExpressionType.Constant)
                return exp;

            return new TypeImplementedByAllExpression(QueryBinder.ExtractTypeId(exp));
        }

        protected internal override Expression VisitEntity(EntityExpression entity)
        {
            var newID = (PrimaryKeyExpression)Visit(entity.ExternalId);

            return new EntityExpression(entity.Type, newID, null, null, null, null, null, entity.AvoidExpandOnRetrieving); // remove bindings
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Coalesce)
            {
                if (node.Left.Type.IsLite() || node.Right.Type.IsLite())
                    return this.Visit(new LiteReferenceExpression(node.Type,
                        Expression.Coalesce(GetLiteEntity(node.Left), GetLiteEntity(node.Right)), null, false, false));

                if (typeof(IEntity).IsAssignableFrom(node.Left.Type) || typeof(IEntity).IsAssignableFrom(node.Right.Type))
                {
                    return this.CombineEntities(node.Left, node.Right, node.Type, 
                        (l, r) => Expression.Coalesce(l, r), 
                        (l, r) => Expression.Condition(Expression.NotEqual(node.Left, Expression.Constant(null, node.Left.Type)), l, r));
                }
            }

            return base.VisitBinary(node);
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            if (node.IfTrue.Type.IsLite() || node.IfTrue.Type.IsLite())
                return this.Visit(new LiteReferenceExpression(node.Type,
                    Expression.Condition(node.Test, GetLiteEntity(node.IfTrue), GetLiteEntity(node.IfFalse)), null, false, false));

            if (typeof(IEntity).IsAssignableFrom(node.IfTrue.Type) || typeof(IEntity).IsAssignableFrom(node.IfFalse.Type))
            {
                return this.CombineEntities(node.IfTrue, node.IfFalse, node.Type, 
                    (t, f) => Expression.Condition(node.Test, t, f));
            }

            return base.VisitConditional(node);
        }


        Expression CombineEntities(Expression a, Expression b, Type type, Func<Expression, Expression, Expression> combiner, Func<Expression, Expression, Expression>? combinerIB = null)
        {
            if (a is ImplementedByAllExpression || b is ImplementedByAllExpression)
                return CombineIBA(ToIBA(a, type), ToIBA(b, type), type, combiner);

            if (a is ImplementedByExpression || b is ImplementedByExpression)
                return CombineIB(ToIB(a, type), ToIB(b, type), type, combinerIB ?? combiner);

            if (a is EntityExpression || b is EntityExpression)
            {
                if (a.Type == b.Type)
                    return CombineEntity(ToEntity(a, type), ToEntity(b, type), type, combiner);
                else
                    return CombineIB(ToIB(a, type), ToIB(b, type), type, combiner);
            }

            if (a.IsNull() || b.IsNull())
                return a;

            throw new UnexpectedValueException(a);
        }

        private ImplementedByAllExpression CombineIBA(ImplementedByAllExpression a, ImplementedByAllExpression b, Type type, Func<Expression, Expression, Expression> combiner)
        {
            return new ImplementedByAllExpression(type, combiner(a.Id, b.Id),
                new TypeImplementedByAllExpression(new PrimaryKeyExpression(combiner(a.TypeId.TypeColumn, b.TypeId.TypeColumn))), null);
        }

        private ImplementedByExpression CombineIB(ImplementedByExpression a, ImplementedByExpression b, Type type, Func<Expression, Expression, Expression> combiner)
        {
            return new ImplementedByExpression(type, a.Strategy,
                a.Implementations.OuterJoinDictionaryCC(b.Implementations,
                (t, ia, ib) => CombineEntity(ia ?? NullEntity(t), ib ?? NullEntity(t), t, combiner)
                ));
        }

        private EntityExpression CombineEntity(EntityExpression a, EntityExpression b, Type type, Func<Expression, Expression, Expression> combiner)
        {
            return new EntityExpression(type, new PrimaryKeyExpression(combiner(a.ExternalId.Value, b.ExternalId.Value)), null, null, null, null, null, a.AvoidExpandOnRetrieving || b.AvoidExpandOnRetrieving);
        }

        ImplementedByAllExpression ToIBA(Expression node, Type type)
        {
            if (node.IsNull())
                return new ImplementedByAllExpression(type, 
                    Expression.Constant(null, typeof(string)), 
                    new TypeImplementedByAllExpression(new PrimaryKeyExpression(Expression.Constant(null, PrimaryKey.Type(typeof(TypeEntity))))), null);

            if (node is EntityExpression e)
                return new ImplementedByAllExpression(type, 
                    new SqlCastExpression(typeof(string), e.ExternalId.Value), 
                    new TypeImplementedByAllExpression(new PrimaryKeyExpression(QueryBinder.TypeConstant(e.Type))), null);

            if (node is ImplementedByExpression ib)
                return new ImplementedByAllExpression(type,
                    new PrimaryKeyExpression(QueryBinder.Coalesce(ib.Implementations.Values.Select(a => a.ExternalId.ValueType.Nullify()).Distinct().SingleEx(), ib.Implementations.Select(e => e.Value.ExternalId))),
                    new TypeImplementedByAllExpression(new PrimaryKeyExpression(
                     ib.Implementations.Select(imp => new When(imp.Value.ExternalId.NotEqualsNulll(), QueryBinder.TypeConstant(imp.Key))).ToList()
                     .ToCondition(PrimaryKey.Type(typeof(TypeEntity)).Nullify()))), null);

            if (node is ImplementedByAllExpression iba)
                return iba;

            throw new UnexpectedValueException(node);
        }

        ImplementedByExpression ToIB(Expression node, Type type)
        {
            if (node.IsNull())
                return new ImplementedByExpression(type, CombineStrategy.Case, new Dictionary<Type, EntityExpression>());

            if (node is EntityExpression e)
                return new ImplementedByExpression(type, CombineStrategy.Case, new Dictionary<Type, EntityExpression> { { e.Type, e } });

            if (node is ImplementedByExpression ib)
                return ib;

            throw new UnexpectedValueException(node);
        }

        EntityExpression ToEntity(Expression node, Type type)
        {
            if (node.IsNull())
                return NullEntity(type);

            if (node is EntityExpression e)
                return e;

            throw new UnexpectedValueException(node);
        }

        private static EntityExpression NullEntity(Type type)
        {
            return new EntityExpression(type, new PrimaryKeyExpression(Expression.Constant(null, PrimaryKey.Type(type).Nullify())), null, null, null, null, null, false);
        }

        private Expression GetLiteEntity(Expression lite)
        {
            if (lite is LiteReferenceExpression l)
                return l.Reference;

            if (lite.IsNull())
                return Expression.Constant(null, lite.Type.CleanType());

            if (lite is ConditionalExpression c)
                return Expression.Condition(c.Test, GetLiteEntity(c.IfTrue), GetLiteEntity(c.IfFalse));

            if (lite is BinaryExpression b && b.NodeType == ExpressionType.Coalesce)
                return Expression.Coalesce(GetLiteEntity(b.Left), GetLiteEntity(b.Right));
            
            throw new UnexpectedValueException(lite);
        }
    }

    internal class DistinctEntityCleaner : DbExpressionVisitor
    {
        public static Expression Clean(Expression source)
        {
            DistinctEntityCleaner pc = new DistinctEntityCleaner();
            return pc.Visit(source);
        }

        bool inLite = false;
        protected internal override Expression VisitLiteReference(LiteReferenceExpression lite)
        {
            var oldInLite = inLite;
            inLite = true;
            var result = base.VisitLiteReference(lite);
            inLite = oldInLite;
            return result;
        }

        [return: NotNullIfNotNull("exp")]
        public override Expression? Visit(Expression? exp)
        {
            if (exp == null)
                return null!;

            if (exp.Type == typeof(Type))
                return VisitType(exp);
            else
                return base.Visit(exp);
        }

        private Expression VisitType(Expression exp)
        {
            if (!inLite)
                return exp;

            if (exp.NodeType == ExpressionType.Constant)
                return exp;

            return new TypeImplementedByAllExpression(QueryBinder.ExtractTypeId(exp));
        }

        protected internal override Expression VisitEntity(EntityExpression entity)
        {
            if (!inLite)
                return base.VisitEntity(entity);

            var newID = (PrimaryKeyExpression)Visit(entity.ExternalId);

            return new EntityExpression(entity.Type, newID, null, null, null, null, null, entity.AvoidExpandOnRetrieving); // remove bindings
        }
    }
}
