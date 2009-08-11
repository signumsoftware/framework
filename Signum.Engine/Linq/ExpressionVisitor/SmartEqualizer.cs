using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities;
using Signum.Utilities.Reflection;
using Signum.Engine.Maps;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Linq
{
    public static class SmartEqualizer
    {
        public static Expression EqualNullable(Expression e1, Expression e2)
        {
            if (e1.Type.IsNullable() == e2.Type.IsNullable())
                return Expression.Equal(e1, e2);

            return Expression.Equal(e1.Nullify(), e2.Nullify());
        }

        public static Expression PolymorphicEqual(Expression exp1, Expression exp2)
        {
            return GetKeyArguments(exp1).ZipStrict(GetKeyArguments(exp2), (o, i) => SmartEqualizer.TryEntityEquals(o, i)).Aggregate((a, b) => Expression.And(a, b));
        }

        public static IEnumerable<Expression> GetKeyArguments(Expression keyExpression)
        {
            if (keyExpression.NodeType == ExpressionType.New)
                return (keyExpression as NewExpression).Arguments;
            return new[] { keyExpression };
        }

        public static Expression TryEntityEquals(Expression e1, Expression e2)
        {
            return EntityEquals(e1, e2) ?? EqualNullable(e1, e2);
        }

        internal static Expression EntityIn(Expression newItem, Expression[] expression)
        {
            if(newItem is LazyReferenceExpression)
            {
                newItem = ((LazyReferenceExpression)newItem).Reference;
                expression = expression.Cast<LazyReferenceExpression>().Select(l=>l.Reference).ToArray(); 
            }

            FieldInitExpression fie = newItem as FieldInitExpression;
            if (fie != null)
                return new InExpression(fie.ExternalId, expression.OfType<FieldInitExpression>().Where(fi => fi.Type == fie.Type).Select(a => ((ConstantExpression)a.ExternalId).Value).ToArray());


            return expression.Select(e => EntityEquals(newItem, e)).Where(e => e != False).Aggregate((a, b) => Expression.Or(a, b)); 
        }

        public static Expression EntityEquals(Expression e1, Expression e2)
        {   
            var tE1 = (DbExpressionType)e1.NodeType;
            var tE2 = (DbExpressionType)e2.NodeType;

            if (tE1 == DbExpressionType.LazyReference && tE2 == DbExpressionType.LazyReference)
                return EntityEquals(((LazyReferenceExpression)e1).Reference, ((LazyReferenceExpression)e2).Reference);

            if (tE1 == DbExpressionType.FieldInit)
                if (tE2 == DbExpressionType.FieldInit) return FieFieEquals((FieldInitExpression)e1, (FieldInitExpression)e2);
                else if (tE2 == DbExpressionType.ImplementedBy) return FieIbEquals((FieldInitExpression)e1, (ImplementedByExpression)e2);
                else if (tE2 == DbExpressionType.ImplementedByAll) return FieIbaEquals((FieldInitExpression)e1, (ImplementedByAllExpression)e2);
                else if (tE2 == DbExpressionType.NullEntity) return EqualsToNull(((FieldInitExpression)e1).ExternalId);
                else return null;
            else if (tE1 == DbExpressionType.ImplementedBy)
                if (tE2 == DbExpressionType.FieldInit) return FieIbEquals((FieldInitExpression)e2, (ImplementedByExpression)e1);
                else if (tE2 == DbExpressionType.ImplementedBy) return IbIbEquals((ImplementedByExpression)e1, (ImplementedByExpression)e2);
                else if (tE2 == DbExpressionType.ImplementedByAll) return IbIbaEquals((ImplementedByExpression)e1, (ImplementedByAllExpression)e2);
                else if (tE2 == DbExpressionType.NullEntity) return ((ImplementedByExpression)e1).Implementations.Select(a => EqualsToNull(a.Field)).Aggregate((a, b) => Expression.And(a, b));
                else return null;
            else if (tE1 == DbExpressionType.ImplementedByAll)
                if (tE2 == DbExpressionType.FieldInit) return FieIbaEquals((FieldInitExpression)e2, (ImplementedByAllExpression)e1);
                else if (tE2 == DbExpressionType.ImplementedBy) return IbIbaEquals((ImplementedByExpression)e2, (ImplementedByAllExpression)e1);
                else if (tE2 == DbExpressionType.ImplementedByAll) return IbaIbaEquals((ImplementedByAllExpression)e1, (ImplementedByAllExpression)e2);
                else if (tE2 == DbExpressionType.NullEntity) return EqualsToNull(((ImplementedByAllExpression)e1).ID);
                else return null;
            else if (tE1 == DbExpressionType.NullEntity)
                if (tE2 == DbExpressionType.FieldInit) return EqualsToNull(((FieldInitExpression)e2).ExternalId);
                else if (tE2 == DbExpressionType.ImplementedBy) return ((ImplementedByExpression)e2).Implementations.Select(a => EqualsToNull(a.Field)).Aggregate((a, b) => Expression.And(a, b));
                else if (tE2 == DbExpressionType.ImplementedByAll) return EqualsToNull(((ImplementedByAllExpression)e2).ID);
                else if (tE2 == DbExpressionType.NullEntity) return True;
                else return null;
            else return null;
        }


        static readonly Expression False = Expression.Equal(Expression.Constant(1), Expression.Constant(0));
        static readonly Expression True = Expression.Equal(Expression.Constant(1), Expression.Constant(1));

        private static Expression FieFieEquals(FieldInitExpression fie1, FieldInitExpression fie2)
        {
            if (fie1.Type == fie2.Type)
                return EqualNullable(fie1.ExternalId, fie2.ExternalId);
            else 
                return False;
        }

        private static Expression FieIbEquals(FieldInitExpression fie, ImplementedByExpression ib)
        {
            var imp = ib.Implementations.SingleOrDefault(i => i.Type == fie.Type);
            if (imp == null)
                return False;

            return EqualNullable(imp.Field.ExternalId, fie.ExternalId); 
        }

        private static Expression FieIbaEquals(FieldInitExpression fie, ImplementedByAllExpression iba)
        {
            int id = Schema.Current.IDsForType[fie.Type];

            return Expression.And(EqualNullable(fie.ExternalId, iba.ID), EqualNullable(Expression.Constant(id), iba.TypeID));
        }

        private static Expression IbIbEquals(ImplementedByExpression ib, ImplementedByExpression ib2)
        {
            var list = ib.Implementations.Join(ib2.Implementations, i => i.Type, j => j.Type, (i, j) => EqualNullable(i.Field.ExternalId, j.Field.ExternalId)).ToList();
            if(list.Count == 0)
                return False;
            return list.Aggregate((e1, e2) => Expression.Or(e1, e2));
        }

        private static Expression IbIbaEquals(ImplementedByExpression ib, ImplementedByAllExpression iba)
        {
            var list = ib.Implementations.Select(i => Expression.And(
                EqualNullable(iba.ID, i.Field.ExternalId),
                EqualNullable(iba.TypeID, Expression.Constant(Schema.Current.IDsForType[i.Type])))).ToList();

            if (list.Count == 0)
                return False;

            return list.Aggregate((e1, e2) => Expression.Or(e1, e2));
        }


        private static Expression IbaIbaEquals(ImplementedByAllExpression iba, ImplementedByAllExpression iba2)
        {
            return Expression.And(EqualNullable(iba.ID, iba2.ID), EqualNullable(iba.TypeID, iba2.TypeID)); 
        }

        private static Expression EqualsToNull(Expression exp)
        {
            return EqualNullable(exp, Expression.Constant(null, typeof(int?)));
        }

        
    }
}
