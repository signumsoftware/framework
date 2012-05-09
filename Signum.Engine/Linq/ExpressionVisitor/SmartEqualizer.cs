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
using Signum.Engine.Properties;
using Signum.Entities.Reflection;

namespace Signum.Engine.Linq
{
    internal static class SmartEqualizer
    {
        public static ConstantExpression True = Expression.Constant(true);
        public static ConstantExpression False = Expression.Constant(false);


        public static Expression EqualNullableGroupBy(Expression e1, Expression e2)
        {
            return Expression.Or(Expression.Equal(e1.Nullify(), e2.Nullify()),
                Expression.And(new IsNullExpression(e1), new IsNullExpression(e2)));
        }

        public static Expression EqualNullable(Expression e1, Expression e2)
        {
            if (e1.Type.IsNullable() == e2.Type.IsNullable())
                return Expression.Equal(e1, e2);

            return Expression.Equal(e1.Nullify(), e2.Nullify());
        }

        public static Expression PolymorphicEqual(Expression exp1, Expression exp2)
        {
            if (exp1.NodeType == ExpressionType.New && exp2.NodeType == ExpressionType.New)
            {
                return (exp1 as NewExpression).Arguments.ZipStrict(
                       (exp2 as NewExpression).Arguments, (o, i) => SmartEqualizer.PolymorphicEqual(o, i)).AggregateAnd();
            }

            Expression result;
            result = ConditionalEquals(exp1, exp2);
            if (result != null)
                return result;

            result = CoalesceEquals(exp1, exp2);
            if (result != null)
                return result;

            result = LiteEquals(exp1, exp2);
            if (result != null)
                return result;
            
            result = EntityEquals(exp1, exp2);
            if (result != null)
                return result;

            result = TypeEquals(exp1, exp2);
            if (result != null)
                return result;

            return EqualNullable(exp1, exp2);
        }

        private static Expression ConditionalEquals(Expression exp1, Expression exp2)
        {
            if (Schema.Current.Settings.IsDbType(exp1.Type)||
                Schema.Current.Settings.IsDbType(exp2.Type))
                return null;

            if (exp1.NodeType == ExpressionType.Conditional)
                return DispachConditional((ConditionalExpression)exp1, exp2);

            if (exp2.NodeType == ExpressionType.Conditional)
                return DispachConditional((ConditionalExpression)exp2, exp1);

            return null;
        }

        private static Expression DispachConditional(ConditionalExpression ce, Expression exp)
        {
            var ifTrue = PolymorphicEqual(ce.IfTrue, exp);
            var ifFalse = PolymorphicEqual(ce.IfFalse, exp);

            return SmartOr(SmartAnd(ce.Test, ifTrue), SmartAnd(SmartNot(ce.Test), ifFalse));
        }

        private static Expression CoalesceEquals(Expression exp1, Expression exp2)
        {
            if (Schema.Current.Settings.IsDbType(exp1.Type)||
                Schema.Current.Settings.IsDbType(exp2.Type))
                return null;

            if (exp1.NodeType == ExpressionType.Coalesce)
                return DispachCoalesce((BinaryExpression)exp1, exp2);

            if (exp2.NodeType == ExpressionType.Coalesce)
                return DispachCoalesce((BinaryExpression)exp2, exp1);

            return null;
        }

        private static Expression DispachCoalesce(BinaryExpression be, Expression exp)
        {
            var leftNull = PolymorphicEqual(be.Left, Expression.Constant(null, be.Type));

            var left = PolymorphicEqual(be.Left, exp);
            var right = PolymorphicEqual(be.Right, exp);

            return SmartOr(SmartAnd(SmartNot(leftNull), left), SmartAnd(leftNull, right));
        }

        private static Expression SmartAnd(Expression e1, Expression e2)
        {
            if (e1 == True)
                return e2;

            if (e2 == True)
                return e1;

            if (e1 == False || e2 == False)
                return False;

            return Expression.And(e1, e2); 
        }

        private static Expression SmartNot(Expression e)
        {
            if (e == True)
                return False;

            if (e == False)
                return True;

            return Expression.Not(e);
        }

        private static Expression SmartOr(Expression e1, Expression e2)
        {
            if (e1 == False)
                return e2;

            if (e2 == False)
                return e1;

            if (e1 == True || e2 == True)
                return True;

            return Expression.Or(e1, e2);
        }

        private static Expression TypeEquals(Expression exp1, Expression exp2)
        {
            if (exp1.Type != typeof(Type) || exp2.Type != typeof(Type))
                return null;

            if (exp1.NodeType == ExpressionType.Constant)
            {
                if (exp2.NodeType == ExpressionType.Constant) return TypeConstantConstantEquals((ConstantExpression)exp1, (ConstantExpression)exp2);
                else if (exp2.NodeType == (ExpressionType)DbExpressionType.TypeFieldInit) return TypeConstantFieEquals((ConstantExpression)exp1, (TypeFieldInitExpression)exp2);
                else if (exp2.NodeType == (ExpressionType)DbExpressionType.TypeImplementedBy) return TypeConstantIbEquals((ConstantExpression)exp1, (TypeImplementedByExpression)exp2);
                else if (exp2.NodeType == (ExpressionType)DbExpressionType.TypeImplementedByAll) return TypeConstantIbaEquals((ConstantExpression)exp1, (TypeImplementedByAllExpression)exp2);
            }
            else if (exp1.NodeType == (ExpressionType)DbExpressionType.TypeFieldInit)
            {
                if (exp2.NodeType == ExpressionType.Constant) return TypeConstantFieEquals((ConstantExpression)exp2, (TypeFieldInitExpression)exp1);
                else if (exp2.NodeType == (ExpressionType)DbExpressionType.TypeFieldInit) return TypeFieFieEquals((TypeFieldInitExpression)exp1, (TypeFieldInitExpression)exp2);
                else if (exp2.NodeType == (ExpressionType)DbExpressionType.TypeImplementedBy) return TypeFieIbEquals((TypeFieldInitExpression)exp1, (TypeImplementedByExpression)exp2);
                else if (exp2.NodeType == (ExpressionType)DbExpressionType.TypeImplementedByAll) return TypeFieIbaEquals((TypeFieldInitExpression)exp1, (TypeImplementedByAllExpression)exp2);
            }
            else if (exp1.NodeType == (ExpressionType)DbExpressionType.TypeImplementedBy)
            {
                if (exp2.NodeType == ExpressionType.Constant) return TypeConstantIbEquals((ConstantExpression)exp2, (TypeImplementedByExpression)exp1);
                else if (exp2.NodeType == (ExpressionType)DbExpressionType.TypeFieldInit) return TypeFieIbEquals((TypeFieldInitExpression)exp2, (TypeImplementedByExpression)exp1);
                else if (exp2.NodeType == (ExpressionType)DbExpressionType.TypeImplementedBy) return TypeIbIbEquals((TypeImplementedByExpression)exp1, (TypeImplementedByExpression)exp2);
                else if (exp2.NodeType == (ExpressionType)DbExpressionType.TypeImplementedByAll) return TypeIbIbaEquals((TypeImplementedByExpression)exp1, (TypeImplementedByAllExpression)exp2);
            }
            else if (exp1.NodeType == (ExpressionType)DbExpressionType.TypeImplementedByAll)
            {
                if (exp2.NodeType == ExpressionType.Constant) return TypeConstantIbaEquals((ConstantExpression)exp2, (TypeImplementedByAllExpression)exp1);
                else if (exp2.NodeType == (ExpressionType)DbExpressionType.TypeFieldInit) return TypeFieIbaEquals((TypeFieldInitExpression)exp2, (TypeImplementedByAllExpression)exp1);
                else if (exp2.NodeType == (ExpressionType)DbExpressionType.TypeImplementedBy) return TypeIbIbaEquals((TypeImplementedByExpression)exp2, (TypeImplementedByAllExpression)exp1);
                else if (exp2.NodeType == (ExpressionType)DbExpressionType.TypeImplementedByAll) return TypeIbaIbaEquals((TypeImplementedByAllExpression)exp1, (TypeImplementedByAllExpression)exp2);
            }

            throw new InvalidOperationException("Impossible to resolve '{0}' equals '{1}'".Formato(exp1.NiceToString(), exp2.NiceToString()));
        }

        private static Expression TypeConstantFieEquals(ConstantExpression ce, TypeFieldInitExpression typeFie)
        {
            if (ce.IsNull())
                return EqualsToNull(typeFie.ExternalId);

            if (((Type)ce.Value == typeFie.TypeValue))
                return new IsNotNullExpression(typeFie.ExternalId);

            return False;
        }

        private static Expression TypeConstantIbEquals(ConstantExpression ce, TypeImplementedByExpression typeIb)
        {
            if (ce.IsNull())
            {
                return typeIb.TypeImplementations.Select(imp => EqualsToNull(imp.ExternalId)).AggregateAnd();
            }

            Type type = (Type)ce.Value;

            var typeImp = typeIb.TypeImplementations.SingleOrDefaultEx(imp => imp.Type == type);

            if (typeImp == null)
                return False;

            return new IsNotNullExpression(typeImp.ExternalId);
        }

        private static Expression TypeConstantIbaEquals(ConstantExpression ce, TypeImplementedByAllExpression typeIba)
        {
            if (ce.IsNull())
                return EqualsToNull(typeIba.TypeColumn);

            return EqualNullable(QueryBinder.TypeConstant((Type)ce.Value), typeIba.TypeColumn);
        }

        private static Expression TypeConstantConstantEquals(ConstantExpression c1, ConstantExpression c2)
        {
            if (c1.IsNull())
            {
                if (c2.IsNull()) return True;
                else return False;
            }
            else
            {
                if (c2.IsNull()) return False;

                if (c1.Value.Equals(c2.Value)) return True;
                else return False;
            }
        }

        private static Expression TypeFieFieEquals(TypeFieldInitExpression typeFie1, TypeFieldInitExpression typeFie2)
        {
            if (typeFie1.TypeValue != typeFie2.TypeValue)
                return False;

            return Expression.And(new IsNotNullExpression(typeFie1.ExternalId), new IsNotNullExpression(typeFie2.ExternalId));
        }

        private static Expression TypeFieIbEquals(TypeFieldInitExpression typeFie, TypeImplementedByExpression typeIb)
        {
            var typeImp = typeIb.TypeImplementations.SingleOrDefaultEx(imp => imp.Type == typeFie.TypeValue);

            if (typeImp == null)
                return False;

            return Expression.And(new IsNotNullExpression(typeFie.ExternalId), new IsNotNullExpression(typeImp.ExternalId));
        }

        private static Expression TypeFieIbaEquals(TypeFieldInitExpression typeFie, TypeImplementedByAllExpression typeIba)
        {
            return Expression.And(new IsNotNullExpression(typeFie.ExternalId), EqualNullable(typeIba.TypeColumn, QueryBinder.TypeConstant(typeFie.TypeValue)));
        }

        private static Expression TypeIbaIbaEquals(TypeImplementedByAllExpression t1, TypeImplementedByAllExpression t2)
        {
            return Expression.Equal(t1.TypeColumn, t2.TypeColumn);
        }

        private static Expression TypeIbIbEquals(TypeImplementedByExpression typeIb1, TypeImplementedByExpression typeIb2)
        {
            var joins = (from imp1 in typeIb1.TypeImplementations
                         join imp2 in typeIb2.TypeImplementations on imp1.Type equals imp2.Type
                         select Expression.And(new IsNotNullExpression(imp1.ExternalId), new IsNotNullExpression(imp2.ExternalId))).ToList();

            return joins.AggregateOr();
        }

        private static Expression TypeIbIbaEquals(TypeImplementedByExpression typeIb, TypeImplementedByAllExpression typeIba)
        {
            return typeIb.TypeImplementations
                .Select(imp => Expression.And(new IsNotNullExpression(imp.ExternalId), EqualNullable(typeIba.TypeColumn, QueryBinder.TypeConstant(imp.Type))))
                .AggregateOr();
        }

        internal static Expression TypeIn(Expression typeExpr, IEnumerable<Type> collection)
        {
            if (collection.IsNullOrEmpty())
                return False;

            if (typeExpr.NodeType == ExpressionType.Conditional)
                return DispachConditionalTypesIn((ConditionalExpression)typeExpr, collection);

            if (typeExpr.NodeType == ExpressionType.Constant)
            {
                Type type = (Type)((ConstantExpression)typeExpr).Value;

                return collection.Contains(type) ? True : False;
            }

            if (typeExpr.NodeType == (ExpressionType)DbExpressionType.TypeFieldInit)
            {
                var typeFie = (TypeFieldInitExpression)typeExpr;

                return collection.Contains(typeFie.TypeValue) ? new IsNotNullExpression(typeFie.ExternalId) : (Expression)False;
            }

            if (typeExpr.NodeType == (ExpressionType)DbExpressionType.TypeImplementedBy)
            {
                var typeIb = (TypeImplementedByExpression)typeExpr;

                return typeIb.TypeImplementations.Where(imp => collection.Contains(imp.Type))
                    .Select(imp => (Expression)new IsNotNullExpression(imp.ExternalId)).AggregateOr();
            }

            if (typeExpr.NodeType == (ExpressionType)DbExpressionType.TypeImplementedByAll)
            {
                var typeIba = (TypeImplementedByAllExpression)typeExpr;

                object[] ids = collection.Select(t => (object)QueryBinder.TypeId(t)).ToArray();

                return InExpression.FromValues(typeIba.TypeColumn, ids);
            }

            throw new InvalidOperationException("Impossible to resolve '{0}' in '{1}'".Formato(typeExpr.NiceToString(), collection.ToString(t=>t.TypeName(), ", ")));
        }

        private static Expression DispachConditionalTypesIn(ConditionalExpression ce, IEnumerable<Type> collection)
        {
            var ifTrue = TypeIn(ce.IfTrue, collection);
            var ifFalse = TypeIn(ce.IfFalse, collection);

            return SmartOr(SmartAnd(ce.Test, ifTrue), SmartAnd(SmartNot(ce.Test), ifFalse));
        }

        internal static Expression EntityIn(Expression newItem, IEnumerable<IdentifiableEntity> collection)
        {
            if (collection.IsEmpty())
                return False;

            Dictionary<Type, object[]> entityIDs = collection.AgGroupToDictionary(a => a.GetType(), gr => gr.Select(a => (object)(a.IdOrNull ?? int.MaxValue)).ToArray());

            return EntityIn(newItem, entityIDs);
        }

        internal static Expression EntityIn(LiteReferenceExpression liteReference, IEnumerable<Lite> collection)
        {
            if (collection.IsEmpty())
                return False;

            Dictionary<Type, object[]> entityIDs = collection.AgGroupToDictionary(a => a.RuntimeType, gr => gr.Select(a => (object)(a.IdOrNull ?? int.MaxValue)).ToArray());

            return EntityIn(liteReference.Reference, entityIDs); 
        }

        static Expression EntityIn(Expression newItem, Dictionary<Type, object[]> entityIDs)
        {
            FieldInitExpression fie = newItem as FieldInitExpression;
            if (fie != null)
                return InExpression.FromValues(fie.ExternalId, entityIDs.TryGetC(fie.Type) ?? new object[0]);

            ImplementedByExpression ib = newItem as ImplementedByExpression;
            if (ib != null)
                return ib.Implementations.ToDictionary(a => a.Type, a => a.Field).JoinDictionary(entityIDs,
                    (t, f, values) => Expression.And(new IsNotNullExpression(f.ExternalId), InExpression.FromValues(f.ExternalId, values)))
                    .Values.AggregateOr();

            ImplementedByAllExpression iba = newItem as ImplementedByAllExpression;
            if (iba != null)
                return entityIDs.Select(kvp => Expression.And(
                    EqualNullable(QueryBinder.TypeConstant(kvp.Key), iba.TypeId.TypeColumn),
                    InExpression.FromValues(iba.Id, kvp.Value))).AggregateOr();

            throw new InvalidOperationException("EntityIn not defined for newItem of type {0}".Formato(newItem.Type.Name));
        }

        public static Expression LiteEquals(Expression e1, Expression e2)
        {
            if (e1 is LiteReferenceExpression || e2 is LiteReferenceExpression)
            {
                e1 = ConstantToLite(e1) ?? e1;
                e2 = ConstantToLite(e2) ?? e2;

                e1 = e1.IsNull() ? e1 : ((LiteReferenceExpression)e1).Reference;
                e2 = e2.IsNull() ? e2 : ((LiteReferenceExpression)e2).Reference;

                return PolymorphicEqual(e1, e2); //Conditional and Coalesce could be inside
            }

            return null;
        }

        public static Expression EntityEquals(Expression e1, Expression e2)
        {
            e1 = ConstantToEntity(e1) ?? e1;
            e2 = ConstantToEntity(e2) ?? e2; 

            var tE1 = (DbExpressionType)e1.NodeType;
            var tE2 = (DbExpressionType)e2.NodeType;

            if (tE1 == DbExpressionType.EmbeddedFieldInit && e2.IsNull())
                return EmbeddedNullEquals((EmbeddedFieldInitExpression)e1);
            if (tE2 == DbExpressionType.EmbeddedFieldInit && e1.IsNull())
                return EmbeddedNullEquals((EmbeddedFieldInitExpression)e2);

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
                else if (e2.IsNull()) return ((ImplementedByExpression)e1).Implementations.Select(a => EqualsToNull(a.Field.ExternalId)).AggregateAnd();
                else return null;
            else if (tE1 == DbExpressionType.ImplementedByAll)
                if (tE2 == DbExpressionType.FieldInit) return FieIbaEquals((FieldInitExpression)e2, (ImplementedByAllExpression)e1);
                else if (tE2 == DbExpressionType.ImplementedBy) return IbIbaEquals((ImplementedByExpression)e2, (ImplementedByAllExpression)e1);
                else if (tE2 == DbExpressionType.ImplementedByAll) return IbaIbaEquals((ImplementedByAllExpression)e1, (ImplementedByAllExpression)e2);
                else if (e2.IsNull()) return EqualsToNull(((ImplementedByAllExpression)e1).Id);
                else return null;
            else if (e1.IsNull())
                if (tE2 == DbExpressionType.FieldInit) return EqualsToNull(((FieldInitExpression)e2).ExternalId);
                else if (tE2 == DbExpressionType.ImplementedBy) return ((ImplementedByExpression)e2).Implementations.Select(a => EqualsToNull(a.Field.ExternalId)).AggregateAnd();
                else if (tE2 == DbExpressionType.ImplementedByAll) return EqualsToNull(((ImplementedByAllExpression)e2).Id);
                else if (e2.IsNull()) return True;
                else return null;

            else return null;
        }

        static Expression EmbeddedNullEquals(EmbeddedFieldInitExpression efie)
        {
            if (efie.HasValue == null)
                return False; 

            return Expression.Not(efie.HasValue);
        }

        static Expression FieFieEquals(FieldInitExpression fie1, FieldInitExpression fie2)
        {
            if (fie1.Type == fie2.Type)
                return EqualNullable(fie1.ExternalId, fie2.ExternalId);
            else
                return False;
        }

        static Expression FieIbEquals(FieldInitExpression fie, ImplementedByExpression ib)
        {
            var imp = ib.Implementations.SingleOrDefaultEx(i => i.Type == fie.Type);
            if (imp == null)
                return False;

            return EqualNullable(imp.Field.ExternalId, fie.ExternalId); 
        }

        static Expression FieIbaEquals(FieldInitExpression fie, ImplementedByAllExpression iba)
        {
            return Expression.And(EqualNullable(fie.ExternalId, iba.Id), EqualNullable(QueryBinder.TypeConstant(fie.Type), iba.TypeId.TypeColumn));
        }

        static Expression IbIbEquals(ImplementedByExpression ib, ImplementedByExpression ib2)
        {
            var list = ib.Implementations.Join(ib2.Implementations, i => i.Type, j => j.Type, (i, j) => EqualNullable(i.Field.ExternalId, j.Field.ExternalId)).ToList();

            return list.AggregateOr();
        }

        static Expression IbIbaEquals(ImplementedByExpression ib, ImplementedByAllExpression iba)
        {
            var list = ib.Implementations.Select(i => Expression.And(
                EqualNullable(iba.Id, i.Field.ExternalId),
                EqualNullable(iba.TypeId.TypeColumn, QueryBinder.TypeConstant(i.Field.Type)))).ToList();

            return list.AggregateOr();
        }


        static Expression IbaIbaEquals(ImplementedByAllExpression iba, ImplementedByAllExpression iba2)
        {
            return Expression.And(EqualNullable(iba.Id, iba2.Id), EqualNullable(iba.TypeId.TypeColumn, iba2.TypeId.TypeColumn)); 
        }

        static Expression EqualsToNull(Expression exp)
        {
            return new IsNullExpression(exp);
        }

        public static Expression ConstantToEntity(Expression expression)
        {
            ConstantExpression c = expression as ConstantExpression;
            if (c == null)
                return null;

            if (c.Value == null)
                return c;

            if (c.Type.IsIIdentifiable())
            {
                var ei = (IdentifiableEntity)c.Value; 

                return new FieldInitExpression(
                    ei.GetType(),
                    null,
                    Expression.Constant(ei.IdOrNull ?? int.MinValue),
                    ProjectionToken.External);
            }
            
            return null;
        }

        public static Expression ConstantToLite(Expression expression)
        {
            ConstantExpression c = expression as ConstantExpression;
            if (c == null)
                return null;

            if (c.Value == null)
                return c;

            if (c.Type.IsLite())
            {
                var lite = (Lite)c.Value;

                Expression id = Expression.Constant(lite.IdOrNull ?? int.MinValue);

                Type liteType = lite.GetType();

                FieldInitExpression fie = new FieldInitExpression(lite.RuntimeType, null, id, ProjectionToken.External);

                Type staticType = Lite.Extract(liteType);
                Expression reference = staticType == fie.Type ? (Expression)fie :
                    new ImplementedByExpression(staticType,
                        new[] { new ImplementationColumnExpression(fie.Type, (FieldInitExpression)fie) }.ToReadOnly());

                return new LiteReferenceExpression(liteType,
                    reference, id, Expression.Constant(lite.ToString()), Expression.Constant(lite.RuntimeType), false);
            }

            return null;
        }
    }
}
