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
using Signum.Entities.Reflection;
using System.Reflection;
using Enum = System.Enum;

namespace Signum.Engine.Linq
{
    internal static class SmartEqualizer
    {
        public static ConstantExpression True = Expression.Constant(true);
        public static ConstantExpression False = Expression.Constant(false);


        static ConstantExpression NewId = Expression.Constant("NewID");


        public static Expression EqualNullableGroupBy(Expression e1, Expression e2)
        {
            return Expression.Or(Expression.Equal(e1.Nullify(), e2.Nullify()),
                Expression.And(new IsNullExpression(e1), new IsNullExpression(e2)));
        }

        public static Expression EqualNullable(Expression e1, Expression e2)
        {
            if (e1 == NewId || e2 == NewId)
                return False;

            if (e1.Type.IsNullable() == e2.Type.IsNullable())
                return Expression.Equal(e1, e2);

            return Expression.Equal(e1.Nullify(), e2.Nullify());
        }

        public static Expression NotEqualNullable(Expression e1, Expression e2)
        {
            if (e1.Type.IsNullable() == e2.Type.IsNullable())
                return Expression.NotEqual(e1, e2);

            return Expression.NotEqual(e1.Nullify(), e2.Nullify());
        }

        public static Expression PolymorphicEqual(Expression exp1, Expression exp2)
        {
            if (exp1.NodeType == ExpressionType.New || exp2.NodeType == ExpressionType.New)
            {
                if (exp1.IsNull() || exp2.IsNull())
                    return Expression.Constant(false);

                exp1 = ConstanToNewExpression(exp1) ?? exp1;
                exp2 = ConstanToNewExpression(exp2) ?? exp2;

                return (exp1 as NewExpression).Arguments.ZipStrict(
                       (exp2 as NewExpression).Arguments, (o, i) => SmartEqualizer.PolymorphicEqual(o, i)).AggregateAnd();
            }

            Expression result;
            result = PrimaryKeyEquals(exp1, exp2);
            if (result != null)
                return result;
            
            result = ObjectEquals(exp1, exp2);
            if (result != null)
                return result;

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

            result = MListElementEquals(exp1, exp2);
            if (result != null)
                return result;


            result = EnumEquals(exp1, exp2);
            if (result != null)
                return result;

            return EqualNullable(exp1, exp2);
        }

        private static Expression EnumEquals(Expression exp1, Expression exp2)
        {
            var exp1Clean = RemoveConvertChain(exp1);
            var exp2Clean = RemoveConvertChain(exp2);


            if (exp1 != exp1Clean || exp2 != exp2Clean)
            {
                var type = exp2.Type.IsNullable() ? exp1.Type.Nullify(): exp1.Type;


                return SmartEqualizer.EqualNullable(exp1Clean.TryConvert(type), exp2Clean.TryConvert(type));
            }

            return null;


        }

        private static Expression RemoveConvertChain(Expression exp)
        {

            while (true)
            {
                var newExp = exp.TryRemoveConvert(t => t.UnNullify().IsEnum) ?? exp.TryRemoveConvert(t => ReflectionTools.IsIntegerNumber(t.UnNullify()));
                if (newExp == null)
                    return exp;

                exp = newExp;

            }
        }



        private static Expression ConstanToNewExpression(Expression exp)
        {
            var ce = exp as ConstantExpression;

            if (ce == null)
                return null;

            var type = ce.Value.GetType();

            if (!type.IsAnonymous())
                return null;

            var values = type.GetProperties().ToDictionary(a => a.Name, a => a.GetValue(ce.Value));

            var ci = type.GetConstructors().SingleEx();

            return Expression.New(ci, ci.GetParameters().Select(p => Expression.Constant(values.GetOrThrow(p.Name), p.ParameterType)));
        }

        public static Expression PrimaryKeyEquals(Expression exp1, Expression exp2)
        {
            if (exp1.Type.UnNullify() == typeof(PrimaryKey) || exp2.Type.UnNullify() == typeof(PrimaryKey))
            {
                var left = UnwrapPrimaryKey(exp1);
                var right = UnwrapPrimaryKey(exp2);

                return EqualNullable(left.Nullify(), right.Nullify());
            }

            return null;
        }

        public static Expression ObjectEquals(Expression expr1, Expression expr2)
        {
            if (expr1.Type == typeof(object) && expr2.Type == typeof(object))
            {
                var left = UncastObject(expr1);
                var right = UncastObject(expr2);

                if (left == null && right == null)
                    return null;

                left = left ?? ChangeConstant(expr1, right.Type);
                right = right ?? ChangeConstant(expr2, left.Type);

                if (left == null || right == null)
                    return null;

                return PolymorphicEqual(left, right);
            }

            return null;
        }

        private static Expression ChangeConstant(Expression exp, Type type)
        {
            if (exp is ConstantExpression ce)
            {
                var val = ce.Value;

                if (val == null)
                {
                    if (type.IsNullable() || !type.IsValueType)
                        return Expression.Constant(val, type);
                    else
                        return null;
                }

                if (type.IsAssignableFrom(val.GetType()))
                    return Expression.Constant(val, type);

                return null;
            }

            return null;
        }

        private static Expression UncastObject(Expression expr)
        {
            if (expr.NodeType == ExpressionType.Convert)
                return ((UnaryExpression)expr).Operand;

            return null;
        }

        public static BinaryExpression UnwrapPrimaryKeyBinary(BinaryExpression b)
        {
            if (b.Left.Type.UnNullify() == typeof(PrimaryKey) || b.Right.Type.UnNullify() == typeof(PrimaryKey))
            {
                var left = UnwrapPrimaryKey(b.Left);
                var right = UnwrapPrimaryKey(b.Right);


                if (left.Type.UnNullify() == typeof(Guid))
                {
                    return Expression.MakeBinary(b.NodeType, left.Nullify(), right.Nullify(), true, GuidComparer.GetMethod(b.NodeType));
                }
                else
                {
                    return Expression.MakeBinary(b.NodeType, left.Nullify(), right.Nullify());
                }
            }

            return b;
        }

        static class GuidComparer
        {
            static bool GreaterThan(Guid a, Guid b)
            {
                return a.CompareTo(b) > 0;
            }

            static bool GreaterThanOrEqual(Guid a, Guid b)
            {
                return a.CompareTo(b) >= 0;
            }

            static bool LessThan(Guid a, Guid b)
            {
                return a.CompareTo(b) < 0;
            }

            static bool LessThanOrEqual(Guid a, Guid b)
            {
                return a.CompareTo(b) <= 0;
            }

            public static MethodInfo GetMethod(ExpressionType type)
            {
                switch (type)
                {
                    case ExpressionType.GreaterThan: return ReflectionTools.GetMethodInfo(() => GreaterThan(Guid.Empty, Guid.Empty));
                    case ExpressionType.GreaterThanOrEqual: return ReflectionTools.GetMethodInfo(() => GreaterThanOrEqual(Guid.Empty, Guid.Empty));
                    case ExpressionType.LessThan: return ReflectionTools.GetMethodInfo(() => LessThan(Guid.Empty, Guid.Empty));
                    case ExpressionType.LessThanOrEqual: return ReflectionTools.GetMethodInfo(() => LessThanOrEqual(Guid.Empty, Guid.Empty));
                    case ExpressionType.Equal: return null;
                    case ExpressionType.NotEqual: return null;
                    default: throw new InvalidOperationException("GuidComparer.GetMethod unexpected ExpressionType " + type);
                }
            }
        }

        

        public static Expression UnwrapPrimaryKey(Expression unary)
        {
            if (unary.NodeType == ExpressionType.Convert && unary.Type.UnNullify() == typeof(PrimaryKey))
                return UnwrapPrimaryKey(((UnaryExpression)unary).Operand);

            if (unary is PrimaryKeyExpression pk)
                return pk.Value;

            if (unary is ConstantExpression ce)
            {
                var obj = ce.Value;
                if (obj == null)
                    return Expression.Constant(null, unary.Type);

                return Expression.Constant(((PrimaryKey)obj).Object);
            }

            return unary;
        }

        private static Expression ConditionalEquals(Expression exp1, Expression exp2)
        {
            if (Schema.Current.Settings.IsDbType(exp1.Type)||
                Schema.Current.Settings.IsDbType(exp2.Type))
                return null;

            if (exp1 is ConditionalExpression ce1)
                return DispachConditional(ce1, exp2);

            if (exp2 is ConditionalExpression ce2)
                return DispachConditional(ce2, exp1);

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

            if (exp1 is ConstantExpression c1)
            {
                if (exp2 is ConstantExpression c2) return TypeConstantConstantEquals(c1, c2);
                else if (exp2 is TypeEntityExpression te2) return TypeConstantEntityEquals(c1, te2);
                else if (exp2 is TypeImplementedByExpression tib2) return TypeConstantIbEquals(c1, tib2);
                else if (exp2 is TypeImplementedByAllExpression tiba2) return TypeConstantIbaEquals(c1, tiba2);
            }
            else if (exp1 is TypeEntityExpression te1)
            {
                if (exp2 is ConstantExpression c2) return TypeConstantEntityEquals(c2, te1);
                else if (exp2 is TypeEntityExpression te2) return TypeEntityEntityEquals(te1, te2);
                else if (exp2 is TypeImplementedByExpression tib2) return TypeEntityIbEquals(te1, tib2);
                else if (exp2 is TypeImplementedByAllExpression tiba2) return TypeEntityIbaEquals(te1, tiba2);
            }
            else if (exp1 is TypeImplementedByExpression tib1)
            {
                if (exp2 is ConstantExpression c2) return TypeConstantIbEquals(c2, tib1);
                else if (exp2 is TypeEntityExpression te2) return TypeEntityIbEquals(te2, tib1);
                else if (exp2 is TypeImplementedByExpression tib2) return TypeIbIbEquals(tib1, tib2);
                else if (exp2 is TypeImplementedByAllExpression tiba2) return TypeIbIbaEquals(tib1, tiba2);
            }
            else if (exp1 is TypeImplementedByAllExpression tiba1)
            {
                if (exp2 is ConstantExpression c2) return TypeConstantIbaEquals(c2, tiba1);
                else if (exp2 is TypeEntityExpression te2) return TypeEntityIbaEquals(te2, tiba1);
                else if (exp2 is TypeImplementedByExpression tib2) return TypeIbIbaEquals(tib2, tiba1);
                else if (exp2 is TypeImplementedByAllExpression tiba2) return TypeIbaIbaEquals(tiba1, tiba2);
            }

            throw new InvalidOperationException("Impossible to resolve '{0}' equals '{1}'".FormatWith(exp1.ToString(), exp2.ToString()));
        }

      

        private static Expression TypeConstantEntityEquals(ConstantExpression ce, TypeEntityExpression typeEntity)
        {
            if (ce.IsNull())
                return EqualsToNull(typeEntity.ExternalId);

            if (((Type)ce.Value == typeEntity.TypeValue))
                return NotEqualToNull(typeEntity.ExternalId);

            return False;
        }

        private static Expression TypeConstantIbEquals(ConstantExpression ce, TypeImplementedByExpression typeIb)
        {
            if (ce.IsNull())
            {
                return typeIb.TypeImplementations.Select(imp => EqualsToNull(imp.Value)).AggregateAnd();
            }

            Type type = (Type)ce.Value;

            var externalId = typeIb.TypeImplementations.TryGetC(type);

            return NotEqualToNull(externalId);
        }

        private static Expression TypeConstantIbaEquals(ConstantExpression ce, TypeImplementedByAllExpression typeIba)
        {
            if (ce.IsNull())
                return EqualsToNull(typeIba.TypeColumn);

            return EqualNullable(QueryBinder.TypeConstant((Type)ce.Value), typeIba.TypeColumn.Value);
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

        private static Expression TypeEntityEntityEquals(TypeEntityExpression typeEntity1, TypeEntityExpression typeEntity2)
        {
            if (typeEntity1.TypeValue != typeEntity2.TypeValue)
                return False;

            return Expression.And(NotEqualToNull(typeEntity1.ExternalId), NotEqualToNull(typeEntity2.ExternalId));
        }

        private static Expression TypeEntityIbEquals(TypeEntityExpression typeEntity, TypeImplementedByExpression typeIb)
        {
            var externalId = typeIb.TypeImplementations.TryGetC(typeEntity.TypeValue);

            if (externalId == null)
                return False;

            return Expression.And(NotEqualToNull(typeEntity.ExternalId), NotEqualToNull(externalId));
        }

        private static Expression TypeEntityIbaEquals(TypeEntityExpression typeEntity, TypeImplementedByAllExpression typeIba)
        {
            return Expression.And(NotEqualToNull(typeEntity.ExternalId), EqualNullable(typeIba.TypeColumn, QueryBinder.TypeConstant(typeEntity.TypeValue)));
        }

        private static Expression TypeIbaIbaEquals(TypeImplementedByAllExpression t1, TypeImplementedByAllExpression t2)
        {
            return Expression.Equal(t1.TypeColumn, t2.TypeColumn);
        }

        private static Expression TypeIbIbEquals(TypeImplementedByExpression typeIb1, TypeImplementedByExpression typeIb2)
        {
            var joins = (from imp1 in typeIb1.TypeImplementations
                         join imp2 in typeIb2.TypeImplementations on imp1.Key equals imp2.Key
                         select Expression.And(NotEqualToNull(imp1.Value), NotEqualToNull(imp2.Value))).ToList();

            return joins.AggregateOr();
        }

        private static Expression TypeIbIbaEquals(TypeImplementedByExpression typeIb, TypeImplementedByAllExpression typeIba)
        {
            return typeIb.TypeImplementations
                .Select(imp => Expression.And(NotEqualToNull(imp.Value), EqualNullable(typeIba.TypeColumn, QueryBinder.TypeConstant(imp.Key))))
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

            if (typeExpr is TypeEntityExpression typeEntity)
            {
                return collection.Contains(typeEntity.TypeValue) ? NotEqualToNull(typeEntity.ExternalId) : (Expression)False;
            }

            if (typeExpr is TypeImplementedByExpression typeIb)
            {
                return typeIb.TypeImplementations.Where(imp => collection.Contains(imp.Key))
                    .Select(imp => NotEqualToNull(imp.Value)).AggregateOr();
            }

            if (typeExpr is TypeImplementedByAllExpression typeIba)
            {
                PrimaryKey[] ids = collection.Select(t => QueryBinder.TypeId(t)).ToArray();

                return InPrimaryKey(typeIba.TypeColumn, ids);
            }

            throw new InvalidOperationException("Impossible to resolve '{0}' in '{1}'".FormatWith(typeExpr.ToString(), collection.ToString(t=>t.TypeName(), ", ")));
        }

        public static Expression In(Expression element, object[] values)
        {
            return InExpression.FromValues(DbExpressionNominator.FullNominate(element), values);
        }

        public static Expression InPrimaryKey(Expression element, PrimaryKey[] values)
        {
            var cleanValues = values.Select(a => a.Object).ToArray();

            var cleanElement = SmartEqualizer.UnwrapPrimaryKey(element);

            if (cleanElement == NewId)
                return False;

            return InExpression.FromValues(DbExpressionNominator.FullNominate(cleanElement), cleanValues);
        }

        private static Expression DispachConditionalTypesIn(ConditionalExpression ce, IEnumerable<Type> collection)
        {
            var ifTrue = TypeIn(ce.IfTrue, collection);
            var ifFalse = TypeIn(ce.IfFalse, collection);

            return SmartOr(SmartAnd(ce.Test, ifTrue), SmartAnd(SmartNot(ce.Test), ifFalse));
        }

        internal static Expression EntityIn(Expression newItem, IEnumerable<Entity> collection)
        {
            if (collection.IsEmpty())
                return False;

            Dictionary<Type, PrimaryKey[]> entityIDs = collection.Where(a => a.IdOrNull.HasValue).AgGroupToDictionary(a => a.GetType(), gr => gr.Select(a => a.Id).ToArray());

            return EntityIn(newItem, entityIDs);
        }

        internal static Expression EntityIn(LiteReferenceExpression liteReference, IEnumerable<Lite<IEntity>> collection)
        {
            if (collection.IsEmpty())
                return False;

            Dictionary<Type, PrimaryKey[]> entityIDs = collection.Where(a => a.IdOrNull.HasValue).AgGroupToDictionary(a => a.EntityType, gr => gr.Select(a => a.Id).ToArray());

            return EntityIn(liteReference.Reference, entityIDs); 
        }

        static Expression EntityIn(Expression newItem, Dictionary<Type, PrimaryKey[]> entityIDs)
        {
            if (newItem is EntityExpression ee)
                return InPrimaryKey(ee.ExternalId, entityIDs.TryGetC(ee.Type) ?? new PrimaryKey[0]);

            if (newItem is ImplementedByExpression ib)
                return ib.Implementations.JoinDictionary(entityIDs,
                    (t, f, values) => Expression.And(DbExpressionNominator.FullNominate(NotEqualToNull(f.ExternalId)), InPrimaryKey(f.ExternalId, values)))
                    .Values.AggregateOr();

            if (newItem is ImplementedByAllExpression iba)
                return entityIDs.Select(kvp => Expression.And(
                    EqualNullable(new PrimaryKeyExpression(QueryBinder.TypeConstant(kvp.Key).Nullify()), iba.TypeId.TypeColumn),
                    InPrimaryKey(iba.Id, kvp.Value))).AggregateOr();

            throw new InvalidOperationException("EntityIn not defined for newItem of type {0}".FormatWith(newItem.Type.Name));
        }

      

        public static Expression LiteEquals(Expression e1, Expression e2)
        {
            if ( e1.Type.IsLite() || e2.Type.IsLite())
            {
                if (!e1.Type.IsLite() && !e1.IsNull() || !e2.Type.IsLite() && !e2.IsNull())
                    throw new InvalidOperationException("Imposible to compare expressions of type {0} == {1}".FormatWith(e1.Type.TypeName(), e2.Type.TypeName()));

                return PolymorphicEqual(GetEntity(e1), GetEntity(e2)); //Conditional and Coalesce could be inside
            }

            return null;
        }

        public static Expression MListElementEquals(Expression e1, Expression e2)
        {
            if (e1 is MListElementExpression || e2 is MListElementExpression)
            {
                if (e1.IsNull())
                    return EqualsToNull(((MListElementExpression)e2).RowId);

                if (e2.IsNull())
                    return EqualsToNull(((MListElementExpression)e1).RowId);

                return EqualNullable(((MListElementExpression)e1).RowId, ((MListElementExpression)e2).RowId);
            }

            return null;
        }

        private static Expression GetEntity(Expression liteExp)
        {
            liteExp = ConstantToLite(liteExp) ?? liteExp;

            if (liteExp.IsNull())
                return Expression.Constant(null, liteExp.Type.CleanType());

            if (liteExp is UnaryExpression ue && (ue.NodeType == ExpressionType.Convert || ue.NodeType == ExpressionType.ConvertChecked))
                liteExp = ue.Operand;

            if (!(liteExp is LiteReferenceExpression liteReference))
                throw new InvalidCastException("Impossible to convert expression to Lite: {0}".FormatWith(liteExp.ToString()));

            return liteReference.Reference;
        }

        public static Expression EntityEquals(Expression e1, Expression e2)
        {
            e1 = ConstantToEntity(e1) ?? e1;
            e2 = ConstantToEntity(e2) ?? e2; 

            if (e1 is EmbeddedEntityExpression && e2.IsNull())
                return EmbeddedNullEquals((EmbeddedEntityExpression)e1);
            if (e2 is EmbeddedEntityExpression && e1.IsNull())
                return EmbeddedNullEquals((EmbeddedEntityExpression)e2);

            if (e1 is EntityExpression ee1)
            {
                if (e2 is EntityExpression ee2) return EntityEntityEquals(ee1, ee2);
                else if (e2 is ImplementedByExpression ib2) return EntityIbEquals(ee1, ib2);
                else if (e2 is ImplementedByAllExpression iba2) return EntityIbaEquals(ee1, iba2);
                else if (e2.IsNull()) return EqualsToNull((ee1).ExternalId);
                else return null;
            }
            else if (e1 is ImplementedByExpression ib1)
            {
                if (e2 is EntityExpression ee2) return EntityIbEquals(ee2, ib1);
                else if (e2 is ImplementedByExpression ib2) return IbIbEquals(ib1, ib2);
                else if (e2 is ImplementedByAllExpression iba2) return IbIbaEquals(ib1, iba2);
                else if (e2.IsNull()) return (ib1).Implementations.Select(a => EqualsToNull(a.Value.ExternalId)).AggregateAnd();
                else return null;
            }
            else if (e1 is ImplementedByAllExpression iba1)
            {
                if (e2 is EntityExpression ee2) return EntityIbaEquals(ee2, iba1);
                else if (e2 is ImplementedByExpression ib2) return IbIbaEquals(ib2, iba1);
                else if (e2 is ImplementedByAllExpression iba2) return IbaIbaEquals(iba1, iba2);
                else if (e2.IsNull()) return EqualsToNull((iba1).TypeId.TypeColumn);
                else return null;
            }
            else if (e1.IsNull())
            {
                if (e2 is EntityExpression ee2) return EqualsToNull((ee2).ExternalId);
                else if (e2 is ImplementedByExpression ib2) return ib2.Implementations.Select(a => EqualsToNull(a.Value.ExternalId)).AggregateAnd();
                else if (e2 is ImplementedByAllExpression iba2) return EqualsToNull(iba2.TypeId.TypeColumn);
                else if (e2.IsNull()) return True;
                else return null;
            }

            else return null;
        }

        static Expression EmbeddedNullEquals(EmbeddedEntityExpression eee)
        {
            return Expression.Not(eee.HasValue);
        }

        static Expression EntityEntityEquals(EntityExpression e1, EntityExpression e2)
        {
            if (e1.Type == e2.Type)
                return PolymorphicEqual(e1.ExternalId, e2.ExternalId).And(e1.ExternalPeriod.Overlaps(e2.ExternalPeriod));
            else
                return False;
        }

        static Expression EntityIbEquals(EntityExpression ee, ImplementedByExpression ib)
        {
            var imp = ib.Implementations.TryGetC(ee.Type);
            if (imp == null)
                return False;

            return EntityEntityEquals(imp, ee); 
        }

        static Expression EntityIbaEquals(EntityExpression ee, ImplementedByAllExpression iba)
        {
            return Expression.And(
                ee.ExternalId.Value == NewId ? False : EqualNullable(new SqlCastExpression(typeof(string), ee.ExternalId.Value), iba.Id),
                EqualNullable(QueryBinder.TypeConstant(ee.Type), iba.TypeId.TypeColumn.Value))
                .And(ee.ExternalPeriod.Overlaps(iba.ExternalPeriod));
        }

        static Expression IbIbEquals(ImplementedByExpression ib, ImplementedByExpression ib2)
        {
            var list = ib.Implementations.JoinDictionary(ib2.Implementations, (t, i, j) => EntityEntityEquals(i, j)).Values.ToList();

            return list.AggregateOr();
        }

        static Expression IbIbaEquals(ImplementedByExpression ib, ImplementedByAllExpression iba)
        {
            var list = ib.Implementations.Values.Select(i =>
                Expression.And(
                i.ExternalId.Value == NewId ? (Expression)False : EqualNullable(iba.Id, new SqlCastExpression(typeof(string), i.ExternalId.Value)),
                EqualNullable(iba.TypeId.TypeColumn.Value, QueryBinder.TypeConstant(i.Type)))).ToList();

            return list.AggregateOr();
        }


        static Expression IbaIbaEquals(ImplementedByAllExpression iba, ImplementedByAllExpression iba2)
        {
            return Expression.And(EqualNullable(iba.Id, iba2.Id), EqualNullable(iba.TypeId.TypeColumn.Value, iba2.TypeId.TypeColumn.Value)); 
        }

        static Expression EqualsToNull(PrimaryKeyExpression exp)
        {
            return EqualNullable(exp.Value, new SqlConstantExpression(null, exp.ValueType));
        }

        static Expression NotEqualToNull(PrimaryKeyExpression exp)
        {
            return NotEqualNullable(exp.Value, new SqlConstantExpression(null, exp.ValueType));
        }

        public static Expression ConstantToEntity(Expression expression)
        {
            ConstantExpression c = expression as ConstantExpression;
            if (c == null)
                return null;

            if (c.Value == null)
                return c;

            if (c.Type.IsIEntity())
            {
                var ei = (Entity)c.Value;

                var id = GetPrimaryKeyValue(ei.IdOrNull, ei.GetType());

                return new EntityExpression(ei.GetType(),
                    new PrimaryKeyExpression(id), null, null, null, null, null, avoidExpandOnRetrieving: true);
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
                Lite<IEntity> lite = (Lite<IEntity>)c.Value;

                var id = GetPrimaryKeyValue(lite.IdOrNull, lite.EntityType);

                EntityExpression ere = new EntityExpression(lite.EntityType, new PrimaryKeyExpression(id), null, null, null, null, null, false);

                return new LiteReferenceExpression(Lite.Generate(lite.EntityType), ere, null, false, false);
            }

            return null;
        }

        private static Expression GetPrimaryKeyValue(PrimaryKey? idOrNull, Type type)
        {
            if (idOrNull == null)
                return SmartEqualizer.NewId;

            var pkType = PrimaryKey.Type(type).Nullify();

            if (idOrNull.Value.VariableName != null && PrimaryKeyExpression.PreferVariableNameVariable.Value)
                return new SqlVariableExpression(idOrNull.Value.VariableName, pkType);

            return Expression.Constant(idOrNull.Value.Object, pkType);
        }
    }
}
