using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using System.Data.SqlClient;
using Signum.Utilities;
using System.Diagnostics;
using Signum.Utilities.DataStructures;
using Signum.Entities;
using Signum.Engine;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Collections;
using Signum.Engine.Maps;

namespace Signum.Engine.Linq
{  
    internal static class TranslatorBuilder
    {
        internal static ITranslateResult Build(ProjectionExpression proj)
        {
            Type type = proj.UniqueFunction == null ? proj.Type.ElementType() : proj.Type;

            return miBuildPrivate.GetInvoker(type)(proj);
        }

        static GenericInvoker<Func<ProjectionExpression, ITranslateResult>> miBuildPrivate = new GenericInvoker<Func<ProjectionExpression, ITranslateResult>>(pe => BuildPrivate<int>(pe));

        static TranslateResult<T> BuildPrivate<T>(ProjectionExpression proj)
        {
            var childs = ProjectionGatherer.Gatherer(proj); 
            List<IChildProjection> childProjections = null;
            if (childs.Count > 0)
            {
                childProjections = new List<IChildProjection>();
                foreach (var pChild in childs)
                {
                    IChildProjection item = BuildChild(pChild.Projection);
                    childProjections.Add(item);
                }
            }

            Scope scope = new Scope
            {
                Alias = proj.Source.Alias,
                Positions = proj.Source.Columns.Select((c, i) => new { c.Name, i }).ToDictionary(p => p.Name, p => p.i),
            };

            Expression<Func<IProjectionRow, T>> lambda = ProjectionBuilder.Build<T>(proj.Projector, scope);

            Expression<Func<SqlParameter[]>> createParams;
            string sql = QueryFormatter.Format(proj.Source, out createParams);

            var result = new TranslateResult<T>
            {
                ChildProjections = childProjections,   

                CommandText = sql,

                ProjectorExpression = lambda,

                GetParameters = createParams.Compile(),
                GetParametersExpression = createParams,

                Unique = proj.UniqueFunction,
            };

            return result;
        }

        static IChildProjection BuildChild(ProjectionExpression proj)
        {
            Type type = proj.UniqueFunction == null ? proj.Type.ElementType() : proj.Type;

            if(!type.IsInstantiationOf(typeof(KeyValuePair<,>)))
                throw new InvalidOperationException("All child projections should create KeyValuePairs");

            return miBuildChildPrivate.GetInvoker(type.GetGenericArguments())(proj);
        }

        static GenericInvoker<Func<ProjectionExpression, IChildProjection>> miBuildChildPrivate = new GenericInvoker<Func<ProjectionExpression, IChildProjection>>(proj => BuildChildPrivate<int, bool>(proj));

        static ChildProjection<K, V> BuildChildPrivate<K, V>(ProjectionExpression proj)
        {
            Scope scope = new Scope
            {
                Alias = proj.Source.Alias,
                Positions = proj.Source.Columns.Select((c, i) => new { c.Name, i }).ToDictionary(p => p.Name, p => p.i),
            };

            Expression<Func<IProjectionRow, KeyValuePair<K, V>>> lambda = ProjectionBuilder.Build<KeyValuePair<K, V>>(proj.Projector, scope);

            Expression<Func<SqlParameter[]>> createParams;
            string sql = QueryFormatter.Format(proj.Source, out createParams);

            var result = new ChildProjection<K, V>
            {
                Name = proj.Token,

                CommandText = sql,
                ProjectorExpression = lambda,

                GetParameters = createParams.Compile(),
                GetParametersExpression = createParams,
            };

            return result;
        }

        public static CommandResult BuildCommandResult(CommandExpression command)
        {
            Expression<Func<SqlParameter[]>> createParams;
            string sql = QueryFormatter.Format(command, out createParams);

            return new CommandResult
            {
                GetParameters = createParams.Compile(),
                GetParametersExpression = createParams,
                CommandText = sql,
            }; 
        }

        public class ProjectionGatherer : DbExpressionVisitor
        {
            List<ChildProjectionExpression> list = new List<ChildProjectionExpression>();

            public static List<ChildProjectionExpression> Gatherer(ProjectionExpression proj)
            {
                ProjectionGatherer pg = new ProjectionGatherer();

                pg.Visit(proj);


                return pg.list; 
            }

            protected override Expression VisitChildProjection(ChildProjectionExpression child)
            {
                var result =  base.VisitChildProjection(child);

                list.Add(child);

                return result;
            }
        }

        /// <summary>
        /// ProjectionBuilder is a visitor that converts an projector expression
        /// that constructs result objects out of ColumnExpressions into an actual
        /// LambdaExpression that constructs result objects out of accessing fields
        /// of a ProjectionRow
        /// </summary>
        public class ProjectionBuilder : DbExpressionVisitor
        {
            static ParameterExpression row = Expression.Parameter(typeof(IProjectionRow), "row");

            static PropertyInfo piRetriever = ReflectionTools.GetPropertyInfo((IProjectionRow r) => r.Retriever);
            static MemberExpression retriever = Expression.Property(row, piRetriever); 
           
            public PropertyInfo piToStrLite = ReflectionTools.GetPropertyInfo((Lite l) =>l.ToStr);
            static FieldInfo fiId = ReflectionTools.GetFieldInfo((IdentifiableEntity i) => i.id);

            static MethodInfo miCached = ReflectionTools.GetMethodInfo((IRetriever r) => r.Cached<TypeDN>(null, null)).GetGenericMethodDefinition();
            static MethodInfo miRequest = ReflectionTools.GetMethodInfo((IRetriever r) => r.Request<TypeDN>(null)).GetGenericMethodDefinition();
            static MethodInfo miRequestIBA = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestIBA<TypeDN>(1, 1)).GetGenericMethodDefinition();
            static MethodInfo miRequestLiteIBA = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestLiteIBA<TypeDN>(1, 1)).GetGenericMethodDefinition();

            static MethodInfo miGetType = ReflectionTools.GetMethodInfo((Schema s) => s.GetType(1));

            Scope scope; 
        

            static internal Expression<Func<IProjectionRow, T>> Build<T>(Expression expression, Scope scope)
            {
                ProjectionBuilder pb = new ProjectionBuilder() { scope = scope };
                Expression body = pb.Visit(expression);
                return Expression.Lambda<Func<IProjectionRow, T>>(body, row);
            }

            Expression NullifyColumn(Expression exp)
            {
                ColumnExpression ce = exp as ColumnExpression;
                if (ce == null)
                    return exp;

                if (ce.Type.IsNullable() || ce.Type.IsClass)
                    return ce;

                return new ColumnExpression(ce.Type.Nullify(), ce.Alias, ce.Name);
            }

            protected override Expression VisitUnary(UnaryExpression u)
            {
                if (u.NodeType == ExpressionType.Convert && u.Operand is ColumnExpression && DiffersInNullability(u.Type, u.Operand.Type))
                {
                    ColumnExpression column = (ColumnExpression)u.Operand;
                    return scope.GetColumnExpression(row, column.Alias, column.Name, u.Type);
                }

                return base.VisitUnary(u);
            }

            bool DiffersInNullability(Type a, Type b)
            {
                return
                    a.IsValueType && a.Nullify() == b ||
                    b.IsValueType && b.Nullify() == a;
            }

            protected override Expression VisitColumn(ColumnExpression column)
            {
                return scope.GetColumnExpression(row, column.Alias, column.Name, column.Type);
            }

            protected override Expression VisitChildProjection(ChildProjectionExpression child)
            {
                Expression outer = Visit(child.OuterKey);

                if (outer != child.OuterKey)
                    child = new ChildProjectionExpression(child.Projection, outer); 

                return scope.Lookup(row, child);
            }

            protected override Expression VisitProjection(ProjectionExpression proj)
            {
                throw new InvalidOperationException("No ProjectionExpressions expected at this stage"); 
            }

            protected override Expression VisitFieldInit(FieldInitExpression fieldInit)
            {
                Expression id = Visit(NullifyColumn(fieldInit.ExternalId));

                if (fieldInit.TableAlias == null)
                    return Expression.Call(retriever, miRequest.MakeGenericMethod(fieldInit.Type), id);

                ParameterExpression e = Expression.Parameter(fieldInit.Type, fieldInit.Type.Name.ToLower().Substring(0, 1));

                var block = Expression.Block(
                    fieldInit.Bindings
                    .Where(a => !ReflectionTools.FieldEquals(FieldInitExpression.IdField, a.FieldInfo))
                    .Select(b => Expression.Assign(
                        Expression.Field(e, b.FieldInfo),
                        ExpressionTools.Convert(Visit(b.Binding), b.FieldInfo.FieldType)
                    )));

                LambdaExpression lambda = Expression.Lambda(typeof(Action<>).MakeGenericType(fieldInit.Type), block, e);

                return Expression.Call(retriever, miCached.MakeGenericMethod(fieldInit.Type), id, lambda);
            }

            static PropertyInfo piModified = ReflectionTools.GetPropertyInfo((ModifiableEntity me) => me.Modified);

            static MemberBinding resetModified = Expression.Bind(piModified, Expression.Constant(null, typeof(bool?)));

            protected override Expression VisitEmbeddedFieldInit(EmbeddedFieldInitExpression efie)
            {
                Expression ctor = Expression.MemberInit(Expression.New(efie.Type),
                       efie.Bindings.Select(b => Expression.Bind(b.FieldInfo, Visit(b.Binding))).And(resetModified));
                
                if (efie.HasValue == null)
                    return ctor;

                return Expression.Condition(Expression.Equal(Visit(efie.HasValue), Expression.Constant(true)), ctor, Expression.Constant(null, ctor.Type));
            }

            protected override Expression VisitImplementedBy(ImplementedByExpression rb)
            {
                return rb.Implementations.Select(fie => new When(Visit(fie.Field.ExternalId).NotEqualsNulll(), Visit(fie.Field))).ToCondition(rb.Type);
            }

            protected override Expression VisitImplementedByAll(ImplementedByAllExpression rba)
            {
                return Expression.Call(retriever, miRequestIBA.MakeGenericMethod(rba.Type),
                    Visit(NullifyColumn(rba.Id)),
                    Visit(NullifyColumn(rba.TypeId)));
            }

            protected override Expression VisitLiteReference(LiteReferenceExpression lite)
            {
                var id = Visit(NullifyColumn(lite.Id));
                var toStr = Visit(lite.ToStr);
                var typeId = Visit(NullifyColumn(lite.TypeId));

                Type liteType = Reflector.ExtractLite(lite.Type);

                if (id == null)
                    return Expression.Constant(null, lite.Type);
                else if (toStr == null)
                {
                    return Expression.Call(retriever, miRequestLiteIBA.MakeGenericMethod(liteType), id.Nullify(), typeId.Nullify());
                }
                else
                {
                    int? constantTypeId = ConstantValue(typeId);

                    Expression type = constantTypeId.HasValue ? 
                            (Expression) Expression.Constant(Schema.Current.GetType(constantTypeId.Value)):
                            (Expression) Expression.Call(Expression.Constant(Schema.Current), miGetType, typeId.UnNullify());

                    NewExpression liteConstructor;
                    if (type.NodeType == ExpressionType.Constant && (Type)(((ConstantExpression)type).Value) == liteType)
                    {
                        ConstructorInfo ciLite = lite.Type.GetConstructor(new[] { typeof(int), typeof(string) });
                        liteConstructor = Expression.New(ciLite, id.UnNullify(), toStr);
                    }
                    else
                    {
                        ConstructorInfo ciLite = lite.Type.GetConstructor(new[] { typeof(Type), typeof(int), typeof(string) });
                        liteConstructor = Expression.New(ciLite, type, id.UnNullify(), toStr);
                    }

                    return Expression.Condition(id.NotEqualsNulll(), liteConstructor, Expression.Constant(null, lite.Type));
                }
            }

            protected override Expression VisitMListElement(MListElementExpression mle)
            {
                Type type = mle.Type;

                return Expression.MemberInit(Expression.New(type),
                    Expression.Bind(type.GetProperty("RowId"), Visit(mle.RowId)),
                    Expression.Bind(type.GetProperty("Parent"), Visit(mle.Parent)),
                    Expression.Bind(type.GetProperty("Element"), Visit(mle.Element)));
            }

            private int? ConstantValue(Expression typeId)
            {
                if (typeId.NodeType == ExpressionType.Convert)
                    typeId = ((UnaryExpression)typeId).Operand;

                if (typeId.NodeType == ExpressionType.Constant)
                    return (int)((ConstantExpression)typeId).Value;

                return null;
            }

            static ConstructorInfo ciLite = ReflectionTools.GetConstuctorInfo(() => new Lite<IdentifiableEntity>(typeof(IdentifiableEntity), 2, ""));

            protected override Expression VisitSqlConstant(SqlConstantExpression sce)
            {
                return Expression.Constant(sce.Value, sce.Type);
            }

            protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunction)
            {
                if (sqlFunction.SqlFunction == SqlFunction.COALESCE.ToString())
                {
                    var result = sqlFunction.Arguments.Select(a => Visit(a.Nullify())).Aggregate((a, b) => Expression.Coalesce(a, b));

                    if (!sqlFunction.Type.IsNullable())
                        return result.UnNullify();
                    return result; 
                }

                return base.VisitSqlFunction(sqlFunction);
            }

            
        }
    }

    internal class Scope
    {
        public Alias Alias;

        public Dictionary<string, int> Positions;

        static PropertyInfo miReader = ReflectionTools.GetPropertyInfo((IProjectionRow row) => row.Reader);

        public Expression GetColumnExpression(Expression row, Alias alias, string name, Type type)
        {
            if (alias != Alias)
                throw new InvalidOperationException("alias '{0}' not found".Formato(alias));

            int position = Positions.GetOrThrow(name, "column name '{0}' not found in alias '" + alias + "'");

            return FieldReader.GetExpression(Expression.Property(row, miReader), position, type);
        }

        static MethodInfo miLookup = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.Lookup<int, double>(null, 0)).GetGenericMethodDefinition();
        
        public Expression Lookup(Expression row, ChildProjectionExpression cProj)
        {
            Type t = cProj.Projection.UniqueFunction == null ? cProj.Type.ElementType() : cProj.Type;

            MethodInfo mi = miLookup.MakeGenericMethod(cProj.OuterKey.Type, t);

            Expression call = Expression.Call(row, mi, Expression.Constant(cProj.Projection.Token), cProj.OuterKey);

            if (cProj.Projection.UniqueFunction == null)
            {
                return call;
            }

            MethodInfo miUnique = UniqueMethod(cProj.Projection.UniqueFunction.Value); 
            return Expression.Call(miUnique.MakeGenericMethod(t), call);
        }


        static MethodInfo miSingle = ReflectionTools.GetMethodInfo(() => Enumerable.Single<int>(null)).GetGenericMethodDefinition();
        static MethodInfo miSingleOrDefault = ReflectionTools.GetMethodInfo(() => Enumerable.SingleOrDefault<int>(null)).GetGenericMethodDefinition();
        static MethodInfo miFirst = ReflectionTools.GetMethodInfo(() => Enumerable.First<int>(null)).GetGenericMethodDefinition();
        static MethodInfo miFirstOrDefault = ReflectionTools.GetMethodInfo(() => Enumerable.FirstOrDefault<int>(null)).GetGenericMethodDefinition();

        internal MethodInfo UniqueMethod(UniqueFunction uniqueFunction)
        {
            switch (uniqueFunction)
            {
                case UniqueFunction.First: return miFirst;
                case UniqueFunction.FirstOrDefault: return miFirstOrDefault;
                case UniqueFunction.Single: return miSingle;
                case UniqueFunction.SingleOrDefault: return miSingleOrDefault;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
