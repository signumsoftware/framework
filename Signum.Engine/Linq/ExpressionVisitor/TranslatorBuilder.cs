using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
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
using System.Data.Common;
using Signum.Entities.Basics;
using System.Collections.Concurrent;

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
            var eagerChildProjections = EagerChildProjectionGatherer.Gatherer(proj).Select(cp => BuildChild(cp)).ToList();
            var lazyChildProjections = LazyChildProjectionGatherer.Gatherer(proj).Select(cp => BuildChild(cp)).ToList();

            Scope scope = new Scope
            {
                Alias = proj.Select.Alias,
                Positions = proj.Select.Columns.Select((c, i) => new { c.Name, i }).ToDictionary(p => p.Name, p => p.i),
            };

            Expression<Func<IProjectionRow, T>> lambda = ProjectionBuilder.Build<T>(proj.Projector, scope);

            Expression<Func<DbParameter[]>> createParams;
            string sql = QueryFormatter.Format(proj.Select, out createParams);

            var result = new TranslateResult<T>
            {
                EagerProjections = eagerChildProjections,
                LazyChildProjections = lazyChildProjections,

                CommandText = sql,

                ProjectorExpression = lambda,

                GetParameters = createParams.Compile(),
                GetParametersExpression = createParams,

                Unique = proj.UniqueFunction,
            };

            return result;
        }

        static IChildProjection BuildChild(ChildProjectionExpression childProj)
        {
            var proj = childProj.Projection;

            Type type = proj.UniqueFunction == null ? proj.Type.ElementType() : proj.Type;

            if(!type.IsInstantiationOf(typeof(KeyValuePair<,>)))
                throw new InvalidOperationException("All child projections should create KeyValuePairs");

            return miBuildChildPrivate.GetInvoker(type.GetGenericArguments())(childProj);
        }

        static GenericInvoker<Func<ChildProjectionExpression, IChildProjection>> miBuildChildPrivate = new GenericInvoker<Func<ChildProjectionExpression, IChildProjection>>(proj => BuildChildPrivate<int, bool>(proj));

        static IChildProjection BuildChildPrivate<K, V>(ChildProjectionExpression childProj)
        {
            var proj = childProj.Projection;

            Scope scope = new Scope
            {
                Alias = proj.Select.Alias,
                Positions = proj.Select.Columns.Select((c, i) => new { c.Name, i }).ToDictionary(p => p.Name, p => p.i),
            };

            Expression<Func<IProjectionRow, KeyValuePair<K, V>>> lambda = ProjectionBuilder.Build<KeyValuePair<K, V>>(proj.Projector, scope);

            Expression<Func<DbParameter[]>> createParamsExpression;
            string sql = QueryFormatter.Format(proj.Select, out createParamsExpression);
            Func<DbParameter[]> createParams = createParamsExpression.Compile();

            if (childProj.IsLazyMList)
                return new LazyChildProjection<K, V>
                {
                    Token = childProj.Token,

                    CommandText = sql,
                    ProjectorExpression = lambda,

                    GetParameters = createParams,
                    GetParametersExpression = createParamsExpression,
                };
            else
                return new EagerChildProjection<K, V>
                {
                    Token = childProj.Token,

                    CommandText = sql,
                    ProjectorExpression = lambda,

                    GetParameters = createParams,
                    GetParametersExpression = createParamsExpression,
                };
        }

        public static CommandResult BuildCommandResult(CommandExpression command)
        {
            Expression<Func<DbParameter[]>> createParams;
            string sql = QueryFormatter.Format(command, out createParams);

            return new CommandResult
            {
                GetParameters = createParams.Compile(),
                GetParametersExpression = createParams,
                CommandText = sql,
            }; 
        }

        public class LazyChildProjectionGatherer : DbExpressionVisitor
        {
            List<ChildProjectionExpression> list = new List<ChildProjectionExpression>();

            public static List<ChildProjectionExpression> Gatherer(ProjectionExpression proj)
            {
                LazyChildProjectionGatherer pg = new LazyChildProjectionGatherer();

                pg.Visit(proj);

                return pg.list; 
            }

            protected override Expression VisitChildProjection(ChildProjectionExpression child)
            {
                if (child.IsLazyMList)
                    list.Add(child);
                
                var result =  base.VisitChildProjection(child);

                return result;
            }
        }

        public class EagerChildProjectionGatherer : DbExpressionVisitor
        {
            List<ChildProjectionExpression> list = new List<ChildProjectionExpression>();

            public static List<ChildProjectionExpression> Gatherer(ProjectionExpression proj)
            {
                EagerChildProjectionGatherer pg = new EagerChildProjectionGatherer();

                pg.Visit(proj);

                return pg.list;
            }

            protected override Expression VisitChildProjection(ChildProjectionExpression child)
            {
                var result = base.VisitChildProjection(child);

                if (!child.IsLazyMList)
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
           
            static FieldInfo fiId = ReflectionTools.GetFieldInfo((IdentifiableEntity i) => i.id);

            static MethodInfo miCached = ReflectionTools.GetMethodInfo((IRetriever r) => r.Complete<TypeDN>(null, null)).GetGenericMethodDefinition();
            static MethodInfo miRequest = ReflectionTools.GetMethodInfo((IRetriever r) => r.Request<TypeDN>(null)).GetGenericMethodDefinition();
            static MethodInfo miRequestIBA = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestIBA<TypeDN>(1, 1)).GetGenericMethodDefinition();
            static MethodInfo miRequestLite = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestLite<TypeDN>(null)).GetGenericMethodDefinition();
            static MethodInfo miEmbeddedPostRetrieving = ReflectionTools.GetMethodInfo((IRetriever r) => r.EmbeddedPostRetrieving<EmbeddedEntity>(null)).GetGenericMethodDefinition();

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
                    child = new ChildProjectionExpression(child.Projection, outer, child.IsLazyMList, child.Type, child.Token); 

                return scope.LookupEager(row, child);
            }

            protected Expression VisitMListChildProjection(ChildProjectionExpression child, MemberExpression field)
            {
                Expression outer = Visit(child.OuterKey);

                if (outer != child.OuterKey)
                    child = new ChildProjectionExpression(child.Projection, outer, child.IsLazyMList, child.Type, child.Token);

                return scope.LookupMList(row, child, field);
            }

            protected override Expression VisitProjection(ProjectionExpression proj)
            {
                throw new InvalidOperationException("No ProjectionExpressions expected at this stage"); 
            }


            protected override Expression VisitEntity(EntityExpression fieldInit)
            {
                Expression id = Visit(NullifyColumn(fieldInit.ExternalId));

                if (fieldInit.TableAlias == null)
                    return Expression.Call(retriever, miRequest.MakeGenericMethod(fieldInit.Type), id);

                ParameterExpression e = Expression.Parameter(fieldInit.Type, fieldInit.Type.Name.ToLower().Substring(0, 1));

                var block = Expression.Block(
                    fieldInit.Bindings
                    .Where(a => !ReflectionTools.FieldEquals(EntityExpression.IdField, a.FieldInfo))
                    .Select(b =>
                        {
                            var field = Expression.Field(e, b.FieldInfo);

                            var value = b.Binding is ChildProjectionExpression ? 
                                VisitMListChildProjection((ChildProjectionExpression)b.Binding, field) :
                                Convert(Visit(b.Binding), b.FieldInfo.FieldType);

                            return Expression.Assign(field, value);
                        }
                    ));

                LambdaExpression lambda = Expression.Lambda(typeof(Action<>).MakeGenericType(fieldInit.Type), block, e);

                return Expression.Call(retriever, miCached.MakeGenericMethod(fieldInit.Type), id, lambda);
            }

            private Expression Convert(Expression expression, Type type)
            {
                if (expression.Type == type)
                    return expression;

                return Expression.Convert(expression, type); 
            }

            static PropertyInfo piModified = ReflectionTools.GetPropertyInfo((ModifiableEntity me) => me.Modified);

            static MemberBinding resetModified = Expression.Bind(piModified, Expression.Constant(null, typeof(bool?)));

            protected override Expression VisitEmbeddedEntity(EmbeddedEntityExpression eee)
            {
                Expression ctor = Expression.MemberInit(Expression.New(eee.Type),
                       eee.Bindings.Select(b => Expression.Bind(b.FieldInfo, Visit(b.Binding))).And(resetModified));

                var entity = Expression.Call(retriever, miEmbeddedPostRetrieving.MakeGenericMethod(eee.Type), ctor);

                if (eee.HasValue == null)
                    return entity;

                return Expression.Condition(Expression.Equal(Visit(eee.HasValue.Nullify()), Expression.Constant(true, typeof(bool?))), entity, Expression.Constant(null, ctor.Type));
            }

            protected override Expression VisitImplementedBy(ImplementedByExpression rb)
            {
                return rb.Implementations.Select(ee => new When(Visit(ee.Reference.ExternalId).NotEqualsNulll(), Visit(ee.Reference))).ToCondition(rb.Type);
            }

            protected override Expression VisitImplementedByAll(ImplementedByAllExpression rba)
            {
                return Expression.Call(retriever, miRequestIBA.MakeGenericMethod(rba.Type),
                    Visit(NullifyColumn(rba.Id)),
                    Visit(NullifyColumn(rba.TypeId.TypeColumn)));
            }

            static readonly ConstantExpression NullType = Expression.Constant(null, typeof(Type));
            static readonly ConstantExpression NullId = Expression.Constant(null, typeof(int?));

            protected override Expression VisitTypeFieldInit(TypeEntityExpression typeFie)
            {
                return Expression.Condition(
                    Expression.NotEqual(Visit(NullifyColumn(typeFie.ExternalId)), NullId),
                    Expression.Constant(typeFie.TypeValue, typeof(Type)),
                    NullType);
            }
     
            protected override Expression VisitTypeImplementedBy(TypeImplementedByExpression typeIb)
            {
                return typeIb.TypeImplementations.Reverse().Aggregate((Expression)NullType, (acum, imp) => Expression.Condition(
                    Expression.NotEqual(Visit(NullifyColumn(imp.ExternalId)), NullId),
                    Expression.Constant(imp.Type, typeof(Type)),
                    acum));
            }

            static MethodInfo miGetType = ReflectionTools.GetMethodInfo((Schema s) => s.GetType(1));

            protected override Expression VisitTypeImplementedByAll(TypeImplementedByAllExpression typeIba)
            {
                return Expression.Condition(
                    Expression.NotEqual(Visit(NullifyColumn(typeIba.TypeColumn)), NullId),
                    SchemaGetType(typeIba),
                    NullType);
            }

            private MethodCallExpression SchemaGetType(TypeImplementedByAllExpression typeIba)
            {
                return Expression.Call(Expression.Constant(Schema.Current), miGetType, Visit(typeIba.TypeColumn).UnNullify());
            }

            protected override Expression VisitLite(LiteExpression lite)
            {
                var id = Visit(NullifyColumn(lite.Id));

                if (id == null)
                    return Expression.Constant(null, lite.Type);

                var toStr = Visit(lite.ToStr);
                var typeId = lite.TypeId;

                var toStringOrNull = toStr ?? Expression.Constant(null, typeof(string));

                
                Expression nothing = Expression.Constant(null, lite.Type);
                Expression liteConstructor = null;
                if (typeId.NodeType == (ExpressionType)DbExpressionType.TypeEntity)
                {
                    Type type = ((TypeEntityExpression)typeId).TypeValue;

                    liteConstructor = Expression.Condition(Expression.NotEqual(id, NullId),
                        Expression.Convert(Expression.New(LiteConstructor(type), id.UnNullify(), toStringOrNull), lite.Type),
                        nothing);
                }
                else if (typeId.NodeType == (ExpressionType)DbExpressionType.TypeImplementedBy)
                {
                    TypeImplementedByExpression tib = (TypeImplementedByExpression)typeId;
                    liteConstructor = tib.TypeImplementations.Aggregate(nothing,
                        (acum, ti) =>
                            {
                                var visitId = Visit(NullifyColumn(ti.ExternalId));
                                return Expression.Condition(Expression.NotEqual(visitId, NullId),
                                    Expression.Convert(Expression.New(LiteConstructor(ti.Type), visitId.UnNullify(), toStringOrNull), lite.Type), acum);
                            });
                }
                else if (typeId.NodeType == (ExpressionType)DbExpressionType.TypeImplementedByAll)
                {
                    TypeImplementedByAllExpression tiba = (TypeImplementedByAllExpression)typeId;
                    liteConstructor = Expression.Condition(Expression.NotEqual(id, NullId),
                                    Expression.Convert(Expression.Call(miLiteCreate, SchemaGetType(tiba), id.UnNullify(), toStringOrNull), lite.Type),
                                     nothing);
                }
                else
                {
                    liteConstructor = Expression.Condition(Expression.NotEqual(id, NullId),
                                       Expression.Convert(Expression.Call(miLiteCreate, Visit(typeId), id.UnNullify(), toStringOrNull), lite.Type),
                                        nothing);
                }

                if (toStr != null)
                    return liteConstructor;
                else
                    return Expression.Call(retriever, miRequestLite.MakeGenericMethod(Lite.Extract(lite.Type)), liteConstructor);
            }

            static MethodInfo miLiteCreate = ReflectionTools.GetMethodInfo(() => Lite.Create(null, 0, null));

            static ConcurrentDictionary<Type, ConstructorInfo> ciLiteConstructor = new ConcurrentDictionary<Type,ConstructorInfo>();

            static ConstructorInfo LiteConstructor( Type type)
            {
                return ciLiteConstructor.GetOrAdd(type, t => typeof(LiteImp<>).MakeGenericType(t).GetConstructor(new[] { typeof(int), typeof(string) }));
            }

            protected override Expression VisitMListElement(MListElementExpression mle)
            {
                Type type = mle.Type;

                return Expression.MemberInit(Expression.New(type),
                    Expression.Bind(type.GetProperty("RowId"), Visit(mle.RowId)),
                    Expression.Bind(type.GetProperty("Parent"), Visit(mle.Parent)),
                    Expression.Bind(type.GetProperty("Element"), Visit(mle.Element)));
            }

            private Type ConstantType(Expression typeId)
            {
                if (typeId.NodeType == ExpressionType.Convert)
                    typeId = ((UnaryExpression)typeId).Operand;

                if (typeId.NodeType == ExpressionType.Constant)
                    return (Type)((ConstantExpression)typeId).Value;

                return null;
            }

            protected override Expression VisitSqlConstant(SqlConstantExpression sce)
            {
                return Expression.Constant(sce.Value, sce.Type);
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

        static MethodInfo miLookupRequest = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.LookupRequest<int, double>(null, 0, null)).GetGenericMethodDefinition();
        static MethodInfo miLookup = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.Lookup<int, double>(null, 0)).GetGenericMethodDefinition();

        public Expression LookupEager(Expression row, ChildProjectionExpression cProj)
        {
            if (cProj.IsLazyMList)
                throw new InvalidOperationException("IsLazyMList not expected at this stage");

            Type type = cProj.Projection.UniqueFunction == null ? cProj.Type.ElementType() : cProj.Type;

            MethodInfo mi = miLookup.MakeGenericMethod(cProj.OuterKey.Type, type);

            Expression call = Expression.Call(row, mi, Expression.Constant(cProj.Token), cProj.OuterKey);

            if (cProj.Projection.UniqueFunction != null)
                throw new InvalidOperationException("Eager ChildProyection with UniqueFunction '{0}' not expected at this stage".Formato(cProj.Projection.UniqueFunction));

            return call;
        }

        public Expression LookupMList(Expression row, ChildProjectionExpression cProj, MemberExpression field)
        {
            if (!cProj.IsLazyMList)
                throw new InvalidOperationException("Not IsLazyMList not expected at this stage");

            if (!cProj.Type.IsMList())
                throw new InvalidOperationException("Lazy ChildProyection of type '{0}' instead of MList".Formato(cProj.Type.TypeName()));

            if (cProj.Projection.UniqueFunction != null)
                throw new InvalidOperationException("Lazy ChildProyection with UniqueFunction '{0}'".Formato(cProj.Projection.UniqueFunction));

            MethodInfo mi = miLookupRequest.MakeGenericMethod(cProj.OuterKey.Type, cProj.Type.ElementType());

            return Expression.Call(row, mi, Expression.Constant(cProj.Token), cProj.OuterKey, field);
        }
    }
}
