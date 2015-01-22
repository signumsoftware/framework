using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Presentation;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Cache
{
    class ToStringExpressionVisitor : ExpressionVisitor
    {
        Dictionary<ParameterExpression, Expression> replacements = new Dictionary<ParameterExpression, Expression>(); 

        public static Func<PrimaryKey, string> GetToString<T>(CachedTableConstructor constructor, Expression<Func<T, string>> lambda)
        {
            Table table = (Table)constructor.table;
            
            var param = lambda.Parameters.SingleEx();

            if (param.Type != table.Type)
                throw new InvalidOperationException("incorrect lambda paramer type");

            var pk = Expression.Parameter(typeof(PrimaryKey), "pk");

            var visitor = new ToStringExpressionVisitor
            {
                replacements = { { param, new CachedEntityExpression(pk, typeof(T), constructor, null) } }
            };
            
            var result = visitor.Visit(lambda.Body);

            return Expression.Lambda<Func<PrimaryKey, string>>(result, pk).Compile();
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var exp = this.Visit(node.Expression);

            if (exp is CachedEntityExpression)
            {
                var cee = (CachedEntityExpression)exp;

                Field field = cee.FieldEmbedded != null ? cee.FieldEmbedded.GetField(node.Member) :
                    ((Table)cee.Constructor.table).GetField(node.Member);

                return BindMember(cee, field, cee.PrimaryKey);
            }

            return node.Update(exp);
        }

        private Expression BindMember(CachedEntityExpression n, Field field, Expression prevPrimaryKey)
        {
            Expression body = GetField(field, n.Constructor, prevPrimaryKey);

            var lambda = Expression.Lambda(body, n.Constructor.origin);

            ConstantExpression tab = Expression.Constant(n.Constructor.cachedTable, typeof(CachedTable<>).MakeGenericType(((Table)n.Constructor.table).Type));

            if (!n.PrimaryKey.Type.IsNullable())
            {
                Expression origin = Expression.Convert(Expression.Property(Expression.Property(tab, "Rows"), "Item", n.PrimaryKey), n.Constructor.tupleType);

                return ExpressionReplacer.Replace(body, new Dictionary<ParameterExpression, Expression> { { n.Constructor.origin, origin } });
            }
            else
            {
                var pk2 = Expression.Parameter(typeof(PrimaryKey), "pk2");

                Expression origin = Expression.Convert(Expression.Property(Expression.Property(tab, "Rows"), "Item", pk2), n.Constructor.tupleType);

                var newBody = ExpressionReplacer.Replace(body, new Dictionary<ParameterExpression, Expression> { { n.Constructor.origin, origin } });

                return TryExpression(n.PrimaryKey, pk2, newBody);
            }
        }

        public static readonly MethodInfo miTryCC = ReflectionTools.GetMethodInfo((string s) => s.Try(a => a.ToString())).GetGenericMethodDefinition();
        public static readonly MethodInfo miTryCS = ReflectionTools.GetMethodInfo((string s) => s.Try(a => a.Length)).GetGenericMethodDefinition();
        public static readonly MethodInfo miTryCN = ReflectionTools.GetMethodInfo((string s) => s.Try(a => (int?)a.Length)).GetGenericMethodDefinition();

        public static readonly MethodInfo miTrySC = ReflectionTools.GetMethodInfo((int? s) => s.Try(a => a.ToString())).GetGenericMethodDefinition();
        public static readonly MethodInfo miTrySS = ReflectionTools.GetMethodInfo((int? s) => s.Try(a => a)).GetGenericMethodDefinition();
        public static readonly MethodInfo miTrySN = ReflectionTools.GetMethodInfo((int? s) => s.Try(a => (int?)a)).GetGenericMethodDefinition();

        public static MethodCallExpression TryExpression(Expression left, ParameterExpression param, Expression body)
        {
            MethodInfo mi = left.Type.IsClass ?
                (body.Type.IsClass ? miTryCC : body.Type.IsValueType ? miTryCS : miTryCN) :
                (body.Type.IsClass ? miTrySC : body.Type.IsValueType ? miTrySS : miTrySN);

            MethodInfo miConv = mi.MakeGenericMethod(left.Type.UnNullify(), body.Type.UnNullify());

            Type lambdaType = miConv.GetParameters().Last().ParameterType;

            var lambda = Expression.Lambda(lambdaType, body, param);

            return Expression.Call(miConv, left, lambda);
        }

        private Expression GetField(Field field, CachedTableConstructor constructor, Expression previousPrimaryKey)
        {
            if (field is FieldValue)
            {
                var value = constructor.GetTupleProperty((IColumn)field);
                return value.Type == field.FieldType ? value : Expression.Convert(value, field.FieldType);
            }

            if (field is FieldEnum)
                return Expression.Convert(constructor.GetTupleProperty((IColumn)field), field.FieldType);

            if (field is IFieldReference)
            {   
                bool isLite = ((IFieldReference)field).IsLite;

                if (field is FieldReference)
                {
                    IColumn column = (IColumn)field;

                    Expression id = CachedTableConstructor.WrapPrimaryKey(constructor.GetTupleProperty(column));

                    return GetEntity(isLite, column, field.FieldType.CleanType(),  constructor);
                }

                if (field is FieldImplementedBy)
                {
                    var ib = (FieldImplementedBy)field;
                    var nullRef = Expression.Constant(null, field.FieldType);
                    var call = ib.ImplementationColumns.Aggregate((Expression)nullRef, (acum, kvp) =>
                    {
                        IColumn column = (IColumn)kvp.Value;

                        Expression id = CachedTableConstructor.NewPrimaryKey(constructor.GetTupleProperty(column));

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

            if (field is FieldEmbedded)
            {
                var fe = (FieldEmbedded)field;

                return new CachedEntityExpression(previousPrimaryKey, fe.FieldType, constructor, fe);
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

            var pk = CachedTableConstructor.WrapPrimaryKey(id.UnNullify());

            CachedTableConstructor typeConstructor = CacheLogic.GetCacheType(entityType) == CacheType.Cached ?
                CacheLogic.GetCachedTable(entityType).Constructor :
                constructor.cachedTable.SubTables.SingleEx(a => a.ParentColumn == column).Constructor;

            return new CachedEntityExpression(pk, entityType, typeConstructor, null);
        }

        static readonly MethodInfo miToString = ReflectionTools.GetMethodInfo((object o) => o.ToString());

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "TryToString")
            {
                node = Expression.Call(node.Arguments.SingleEx(), miToString);
            }

            var obj = base.Visit(node.Object);

            var args = base.Visit(node.Arguments);

            LambdaExpression lambda = ExpressionCleaner.GetFieldExpansion(obj.Try(a => a.Type), CachedTableBase.ToStringMethod);

            if (lambda != null)
            {
                var replace = ExpressionReplacer.Replace(Expression.Invoke(lambda, obj == null ? args : args.PreAnd(obj)));

                return this.Visit(replace);
            }

            if (node.Method.Name == "ToString" && node.Arguments.IsEmpty() && obj is CachedEntityExpression)
            {
                var ce = (CachedEntityExpression)obj;
                var table = (Table)ce.Constructor.table;

                return BindMember(ce, (FieldValue)table.ToStrColumn, null);
            }

            return node.Update(obj, args);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return this.replacements.TryGetC(node) ?? node;
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
        public readonly FieldEmbedded FieldEmbedded;

        public readonly Type type;
        public override Type Type { get { return type; } }

        public CachedEntityExpression(Expression primaryKey, Type type, CachedTableConstructor constructor, FieldEmbedded embedded)
        {     
            if (primaryKey == null)
                throw new ArgumentNullException("primaryKey");

            if (primaryKey.Type.UnNullify() != typeof(PrimaryKey))
                throw new InvalidOperationException("primaryKey should be a PrimaryKey");

            if (!type.IsEmbeddedEntity())
            {
                if (((Table)constructor.table).Type != type.CleanType())
                    throw new InvalidOperationException("Wrong type");
            }
            else
            {
                if (embedded == null)
                    throw new ArgumentNullException("embedded");

                this.FieldEmbedded = embedded;
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

            return new CachedEntityExpression(pk, type, Constructor, FieldEmbedded);
        }
    }
}
