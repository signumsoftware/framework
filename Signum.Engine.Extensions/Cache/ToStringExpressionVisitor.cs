using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Cache
{
    class ToStringExpressionVisitor : ExpressionVisitor
    {
        Dictionary<ParameterExpression, Expression> replacements = new Dictionary<ParameterExpression, Expression>();

        CachedEntityExpression root;

        public ToStringExpressionVisitor(ParameterExpression param, CachedEntityExpression root)
        {
            this.root = root;
            this.replacements = new Dictionary<ParameterExpression, Expression> { { param, root } };
        }

        public static Expression<Func<PrimaryKey, string>> GetToString<T>(CachedTableConstructor constructor, Expression<Func<T, string>> lambda)
        {
            Table table = (Table)constructor.table;

            var param = lambda.Parameters.SingleEx();

            if (param.Type != table.Type)
                throw new InvalidOperationException("incorrect lambda paramer type");

            var pk = Expression.Parameter(typeof(PrimaryKey), "pk");

            var root = new CachedEntityExpression(pk, typeof(T), constructor, null, null);

            var visitor = new ToStringExpressionVisitor(param, root);

            var result = visitor.Visit(lambda.Body);

            return Expression.Lambda<Func<PrimaryKey, string>>(result, pk);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var exp = this.Visit(node.Expression);

            if (exp is CachedEntityExpression cee)
            {
                Field field = 
                    cee.FieldEmbedded != null ? cee.FieldEmbedded.GetField(node.Member) :
                    cee.FieldMixin != null ? cee.FieldMixin.GetField(node.Member) :
                    ((Table)cee.Constructor.table).GetField(node.Member);

                return BindMember(cee, field, cee.PrimaryKey);
            }

            return node.Update(exp);
        }

        private Expression BindMember(CachedEntityExpression n, Field field, Expression? prevPrimaryKey)
        {
            Expression body = GetField(field, n.Constructor, prevPrimaryKey);

            ConstantExpression tab = Expression.Constant(n.Constructor.cachedTable, typeof(CachedTable<>).MakeGenericType(((Table)n.Constructor.table).Type));

            Expression origin = Expression.Convert(Expression.Property(Expression.Call(tab, "GetRows", null), "Item", n.PrimaryKey.UnNullify()), n.Constructor.tupleType);

            var result = ExpressionReplacer.Replace(body, new Dictionary<ParameterExpression, Expression> { { n.Constructor.origin, origin } });

            if (!n.PrimaryKey.Type.IsNullable())
                return result;

            return Expression.Condition(
                Expression.Equal(n.PrimaryKey, Expression.Constant(null, n.PrimaryKey.Type)),
                Expression.Constant(null, result.Type.Nullify()),
                result.Nullify());
        }

        private Expression GetField(Field field, CachedTableConstructor constructor, Expression? previousPrimaryKey)
        {
            if (field is FieldValue)
            {
                var value = constructor.GetTupleProperty((IColumn)field);
                return value.Type == field.FieldType ? value : Expression.Convert(value, field.FieldType);
            }

            if (field is FieldEnum)
                return Expression.Convert(constructor.GetTupleProperty((IColumn)field), field.FieldType);

            if (field is FieldPrimaryKey)
                return constructor.GetTupleProperty((IColumn)field);

            if (field is IFieldReference)
            {
                bool isLite = ((IFieldReference)field).IsLite;

                if (field is FieldReference)
                {
                    IColumn column = (IColumn)field;

                    return GetEntity(isLite, column, field.FieldType.CleanType(),  constructor);
                }

                if (field is FieldImplementedBy ib)
                {
                    var nullRef = Expression.Constant(null, field.FieldType);
                    var call = ib.ImplementationColumns.Aggregate((Expression)nullRef, (acum, kvp) =>
                    {
                        IColumn column = (IColumn)kvp.Value;

                        var entity = GetEntity(isLite, column, kvp.Key, constructor);

                        return Expression.Condition(Expression.NotEqual(constructor.GetTupleProperty(column), Expression.Constant(column.Type)),
                            Expression.Convert(entity, field.FieldType),
                            acum);
                    });

                    return call;
                }

                if (field is FieldImplementedByAll)
                {
                    throw new NotImplementedException("FieldImplementedByAll not supported in cached ToString");
                }
            }

            if (field is FieldEmbedded fe)
            {
                return new CachedEntityExpression(previousPrimaryKey!, fe.FieldType, constructor, fe, null);
            }

            if (field is FieldMixin fm)
            {
                return new CachedEntityExpression(previousPrimaryKey!, fm.FieldType, constructor, null, fm);
            }

            if (field is FieldMList)
            {
                throw new NotImplementedException("FieldMList not supported in cached ToString");
            }

            throw new InvalidOperationException("Unexpected {0}".FormatWith(field.GetType().Name));
        }

        private Expression GetEntity(bool isLite, IColumn column, Type entityType, CachedTableConstructor constructor)
        {
            Expression id = constructor.GetTupleProperty(column);

            var pk = CachedTableConstructor.WrapPrimaryKey(id);

            CachedTableConstructor typeConstructor = CacheLogic.GetCacheType(entityType) == CacheType.Cached ?
                CacheLogic.GetCachedTable(entityType).Constructor :
                constructor.cachedTable.SubTables!.SingleEx(a => a.ParentColumn == column).Constructor;

            return new CachedEntityExpression(pk, entityType, typeConstructor, null, null);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            var operand = Visit(node.Operand);
            if (operand != node.Operand && node.NodeType == ExpressionType.Convert)
            {
                return Expression.Convert(operand, node.Type);
            }
            return node.Update(operand);
        }

        static readonly MethodInfo miToString = ReflectionTools.GetMethodInfo((object o) => o.ToString());
        
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(string) && node.Method.Name == nameof(string.Format) ||
              node.Method.DeclaringType == typeof(StringExtensions) && node.Method.Name == nameof(StringExtensions.FormatWith))
            {
                var formatStr = Visit(node.Arguments[0]);
                var remainging = node.Arguments.Skip(1).Select(a => Visit(ToString(a))).ToList();


                return node.Update(null!, new Sequence<Expression> { formatStr, remainging });
            }

            var obj = base.Visit(node.Object);
            var args = base.Visit(node.Arguments);

            if (node.Method.Name == "ToString" && node.Arguments.IsEmpty() && obj is CachedEntityExpression ce && ce.Type.IsEntity())
            {
                var table = (Table)ce.Constructor.table;

                if (table.ToStrColumn != null)
                {
                    return BindMember(ce, (FieldValue)table.ToStrColumn, null);
                }
                else if(this.root != ce)
                {
                    var cachedTableType = typeof(CachedTable<>).MakeGenericType(table.Type);

                    ConstantExpression tab = Expression.Constant(ce.Constructor.cachedTable, cachedTableType);

                    var mi = cachedTableType.GetMethod(nameof(CachedTable<Entity>.GetToString))!;

                    return Expression.Call(tab, mi, ce.PrimaryKey.UnNullify());
                }
            }

            LambdaExpression? lambda = ExpressionCleaner.GetFieldExpansion(obj?.Type, node.Method);

            if (lambda != null)
            {
                var replace = ExpressionReplacer.Replace(Expression.Invoke(lambda, obj == null ? args : args.PreAnd(obj)));

                return this.Visit(replace);
            }

            if (node.Method.Name == nameof(Entity.Mixin) && obj is CachedEntityExpression cee)
            {
                var mixin = ((Table)cee.Constructor.table).GetField(node.Method);

                return GetField(mixin, cee.Constructor, cee.PrimaryKey);
            }

            return node.Update(obj!, args);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return this.replacements.TryGetC(node) ?? node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var result = (BinaryExpression)base.VisitBinary(node);

            if (result.NodeType == ExpressionType.Equal || result.NodeType == ExpressionType.NotEqual)
            {
                if (result.Left is CachedEntityExpression ceLeft && ceLeft.FieldEmbedded?.HasValue == null ||
                    result.Right is CachedEntityExpression ceRight && ceRight.FieldEmbedded?.HasValue == null)
                {
                    var left = GetPrimaryKey(result.Left);
                    var right = GetPrimaryKey(result.Right);

                    if (left.Type.IsNullable() || right.Type.IsNullable())
                        return Expression.MakeBinary(node.NodeType, left.Nullify(), right.Nullify());
                    else
                        return Expression.MakeBinary(node.NodeType, left, right);
                }

                if (result.Left is CachedEntityExpression ceLeft2 && ceLeft2.FieldEmbedded?.HasValue != null ||
                    result.Right is CachedEntityExpression ceRight2 && ceRight2.FieldEmbedded?.HasValue != null)
                {
                    var left = GetHasValue(result.Left);
                    var right = GetHasValue(result.Right);

                    return Expression.MakeBinary(node.NodeType, left, right);
                }
            }

            if(result.NodeType == ExpressionType.Add && (result.Left.Type == typeof(string) || result.Right.Type == typeof(string)))
            {
                var lefto = this.Visit(ToString(result.Left));
                var righto = this.Visit(ToString(result.Right));

                return Expression.Add(lefto, righto, result.Method);
            }
            return result;
        }

        private Expression ToString(Expression node)
        {
            if (node.Type == typeof(string))
                return node;

            return Expression.Condition(
                Expression.Equal(node.Nullify(), Expression.Constant(null, node.Type.Nullify())),
                Expression.Constant(null, typeof(string)),
                Expression.Call(node, miToString));
        }

        private Expression GetPrimaryKey(Expression exp)
        {
            if (exp is ConstantExpression && ((ConstantExpression)exp).Value == null)
                return Expression.Constant(null, typeof(PrimaryKey?));

            if (exp is CachedEntityExpression cee && cee.FieldEmbedded?.HasValue == null)
                return cee.PrimaryKey;

            throw new InvalidOperationException("");
        }

        private Expression GetHasValue(Expression exp)
        {
            if (exp is ConstantExpression && ((ConstantExpression)exp).Value == null)
                return Expression.Constant(false, typeof(bool));

            if (exp is CachedEntityExpression n && n.FieldEmbedded?.HasValue != null)
            {
                var body = n.Constructor.GetTupleProperty(n.FieldEmbedded.HasValue);

                ConstantExpression tab = Expression.Constant(n.Constructor.cachedTable, typeof(CachedTable<>).MakeGenericType(((Table)n.Constructor.table).Type));

                Expression origin = Expression.Convert(Expression.Property(Expression.Call(tab, "GetRows", null), "Item", n.PrimaryKey.UnNullify()), n.Constructor.tupleType);

                var result = ExpressionReplacer.Replace(body, new Dictionary<ParameterExpression, Expression> { { n.Constructor.origin, origin } });

                return result;
            }

            throw new InvalidOperationException("");
        }
    }


    internal class CachedEntityExpression : Expression
    {
        public override ExpressionType NodeType
        {
            get { return ExpressionType.Extension; }
        }

        public readonly CachedTableConstructor Constructor;
        public readonly Expression PrimaryKey;
        public readonly FieldEmbedded? FieldEmbedded;
        public readonly FieldMixin? FieldMixin;

        public readonly Type type;
        public override Type Type { get { return type; } }

        public CachedEntityExpression(Expression primaryKey, Type type, CachedTableConstructor constructor, FieldEmbedded? embedded, FieldMixin? mixin)
        {
            if (primaryKey == null)
                throw new ArgumentNullException(nameof(primaryKey));

            if (primaryKey.Type.UnNullify() != typeof(PrimaryKey))
                throw new InvalidOperationException("primaryKey should be a PrimaryKey");

            if (type.IsEmbeddedEntity())
            {
                this.FieldEmbedded = embedded ?? throw new ArgumentNullException(nameof(embedded));
            }
            else if (type.IsMixinEntity())
            {
                this.FieldMixin = mixin ?? throw new ArgumentNullException(nameof(mixin));
            }
            else
            {
                if (((Table)constructor.table).Type != type.CleanType())
                    throw new InvalidOperationException("Wrong type");
            }

            this.PrimaryKey = primaryKey;

            this.type = type;
            this.Constructor = constructor;
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            if (this.PrimaryKey == null)
                return this;

            var pk = visitor.Visit(this.PrimaryKey);

            if (pk == this.PrimaryKey)
                return this;

            return new CachedEntityExpression(pk, type, Constructor, FieldEmbedded, FieldMixin);
        }

        public override string ToString()
        {
            return $"CachedEntityExpression({Type.TypeName()}, {PrimaryKey})";
        }
    }
}
