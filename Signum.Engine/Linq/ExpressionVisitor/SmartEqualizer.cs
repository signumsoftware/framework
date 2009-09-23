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
    internal static class SmartEqualizer
    {
        public static Expression EqualNullable(Expression e1, Expression e2)
        {
            if (e1.IsNull())
            {
                if (e2.IsNull())
                    return SqlConstantExpression.True;
                else
                    return new IsNullExpression(e2);
            }
            else
            {
                if (e2.IsNull())
                    return new IsNullExpression(e1);
            }

            if (e1.Type.IsNullable() == e2.Type.IsNullable())
                return Expression.Equal(e1, e2);

            return Expression.Equal(e1.Nullify(), e2.Nullify());
        }

        public static Expression PolymorphicEqual(Expression exp1, Expression exp2)
        {
            if (exp1.NodeType == ExpressionType.New && exp2.NodeType == ExpressionType.New)
            {
                return (exp1 as NewExpression).Arguments.ZipStrict(
                       (exp2 as NewExpression).Arguments, (o, i) => SmartEqualizer.TryEntityEquals(o, i)).Aggregate((a, b) => Expression.And(a, b));
            }

            return SmartEqualizer.TryEntityEquals(exp1, exp2);
        }

        public static Expression TryEntityEquals(Expression e1, Expression e2)
        {
            return EntityEquals(e1, e2) ?? EqualNullable(e1, e2);
        }

        internal static Expression EntityIn(Expression newItem, Expression[] expression)
        {
            if(newItem is LazyReferenceExpression)
                return EntityIn(((LazyReferenceExpression)newItem).Reference, expression.Cast<LazyReferenceExpression>().Select(l=>l.Reference).ToArray()); 

            Dictionary<Type, object[]> entityIDs = expression.Cast<FieldInitExpression>().AgGroupToDictionary(a=>a.Type, gr=> gr.Select(a=>((ConstantExpression)a.ExternalId).Value??int.MaxValue).ToArray()); 

            FieldInitExpression fie = newItem as FieldInitExpression;
            if (fie != null)
                return InExpression.FromValues(fie.ExternalId, entityIDs.TryGetC(fie.Type) ?? new object[0]);

            ImplementedByExpression ib = newItem as ImplementedByExpression;
            if (ib != null)
                return ib.Implementations.ToDictionary(a => a.Type, a => a.Field).JoinDictionary(entityIDs, (t, f, values) => Expression.And(
                    new IsNotNullExpression(f.ExternalId),
                    InExpression.FromValues(f.ExternalId, values))).Values.Aggregate((a, b) => Expression.Or(a, b));

            ImplementedByAllExpression iba = newItem as ImplementedByAllExpression;
            if (iba != null)
                return entityIDs.Select(kvp => Expression.And(
                    EqualNullable(Expression.Constant(Schema.Current.IDsForType[kvp.Key]), iba.TypeId),
                    InExpression.FromValues(iba.Id, kvp.Value))).Aggregate((a, b) => Expression.Or(a, b));

            throw new InvalidOperationException("EntityIn not defined for newItem of type  {0}".Formato(newItem.Type.Name));
        }

        public static Expression EntityEquals(Expression e1, Expression e2)
        {   
            if (e1 is LazyReferenceExpression || e2 is LazyReferenceExpression )
            {
                e1 = e1.IsNull() ? e1 : ((LazyReferenceExpression)e1).Reference;
                e2 = e2.IsNull() ? e2 : ((LazyReferenceExpression)e2).Reference;
            }

            var tE1 = (DbExpressionType)e1.NodeType;
            var tE2 = (DbExpressionType)e2.NodeType;

            if (tE1 == DbExpressionType.FieldInit)
                if (tE2 == DbExpressionType.FieldInit) return FieFieEquals((FieldInitExpression)e1, (FieldInitExpression)e2);
                else if (tE2 == DbExpressionType.ImplementedBy) return FieIbEquals((FieldInitExpression)e1, (ImplementedByExpression)e2);
                else if (tE2 == DbExpressionType.ImplementedByAll) return FieIbaEquals((FieldInitExpression)e1, (ImplementedByAllExpression)e2);
                else if (e2.IsNull()) return EqualsToNull(((FieldInitExpression)e1).ExternalId);
                else return null;
            else if (tE1 == DbExpressionType.ImplementedBy)
                if (tE2 == DbExpressionType.FieldInit) return FieIbEquals((FieldInitExpression)e2, (ImplementedByExpression)e1);
                else if (tE2 == DbExpressionType.ImplementedBy) return IbIbEquals((ImplementedByExpression)e1, (ImplementedByExpression)e2);
                else if (tE2 == DbExpressionType.ImplementedByAll) return IbIbaEquals((ImplementedByExpression)e1, (ImplementedByAllExpression)e2);
                else if (e2.IsNull()) return ((ImplementedByExpression)e1).Implementations.Select(a => EqualsToNull(a.Field.ExternalId)).Aggregate((a, b) => Expression.And(a, b));
                else return null;
            else if (tE1 == DbExpressionType.ImplementedByAll)
                if (tE2 == DbExpressionType.FieldInit) return FieIbaEquals((FieldInitExpression)e2, (ImplementedByAllExpression)e1);
                else if (tE2 == DbExpressionType.ImplementedBy) return IbIbaEquals((ImplementedByExpression)e2, (ImplementedByAllExpression)e1);
                else if (tE2 == DbExpressionType.ImplementedByAll) return IbaIbaEquals((ImplementedByAllExpression)e1, (ImplementedByAllExpression)e2);
                else if (e2.IsNull()) return EqualsToNull(((ImplementedByAllExpression)e1).Id);
                else return null;
            else if (e1.IsNull())
                if (tE2 == DbExpressionType.FieldInit) return EqualsToNull(((FieldInitExpression)e2).ExternalId);
                else if (tE2 == DbExpressionType.ImplementedBy) return ((ImplementedByExpression)e2).Implementations.Select(a => EqualsToNull(a.Field.ExternalId)).Aggregate((a, b) => Expression.And(a, b));
                else if (tE2 == DbExpressionType.ImplementedByAll) return EqualsToNull(((ImplementedByAllExpression)e2).Id);
                else if (e2.IsNull()) return SqlConstantExpression.True;
                else return null;
            else return null;
        }

        static Expression FieFieEquals(FieldInitExpression fie1, FieldInitExpression fie2)
        {
            if (fie1.Type == fie2.Type)
                return EqualNullable(fie1.ExternalId, fie2.ExternalId);
            else
                return SqlConstantExpression.False;
        }

        static Expression FieIbEquals(FieldInitExpression fie, ImplementedByExpression ib)
        {
            var imp = ib.Implementations.SingleOrDefault(i => i.Type == fie.Type);
            if (imp == null)
                return SqlConstantExpression.False;

            return EqualNullable(imp.Field.ExternalId, fie.ExternalId); 
        }

        static Expression FieIbaEquals(FieldInitExpression fie, ImplementedByAllExpression iba)
        {
            int id = Schema.Current.IDsForType[fie.Type];

            return Expression.And(EqualNullable(fie.ExternalId, iba.Id), EqualNullable(Expression.Constant(id), iba.TypeId));
        }

        static Expression IbIbEquals(ImplementedByExpression ib, ImplementedByExpression ib2)
        {
            var list = ib.Implementations.Join(ib2.Implementations, i => i.Type, j => j.Type, (i, j) => EqualNullable(i.Field.ExternalId, j.Field.ExternalId)).ToList();
            if(list.Count == 0)
                return SqlConstantExpression.False;
            return list.Aggregate((e1, e2) => Expression.Or(e1, e2));
        }

        static Expression IbIbaEquals(ImplementedByExpression ib, ImplementedByAllExpression iba)
        {
            var list = ib.Implementations.Select(i => Expression.And(
                EqualNullable(iba.Id, i.Field.ExternalId),
                EqualNullable(iba.TypeId, Expression.Constant(Schema.Current.IDsForType[i.Type])))).ToList();

            if (list.Count == 0)
                return SqlConstantExpression.False;

            return list.Aggregate((e1, e2) => Expression.Or(e1, e2));
        }


        static Expression IbaIbaEquals(ImplementedByAllExpression iba, ImplementedByAllExpression iba2)
        {
            return Expression.And(EqualNullable(iba.Id, iba2.Id), EqualNullable(iba.TypeId, iba2.TypeId)); 
        }

        static Expression EqualsToNull(Expression exp)
        {
            return new IsNullExpression(exp);
        }
    }
}
