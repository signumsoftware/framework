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

namespace Signum.Engine.Linq
{  
    internal static class TranslatorBuilder
    {
        internal static ITranslateResult Build(ProjectionExpression proj)
        {
            Type type = proj.UniqueFunction == null ? proj.Type.ElementType() : proj.Type;

            return (ITranslateResult)miBuildPrivate.GenericInvoke(new[] { type }, null, new object[] { proj});
        }

        static MethodInfo miBuildPrivate = ReflectionTools.GetMethodInfo(() => BuildPrivate<int>(null)).GetGenericMethodDefinition();

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

            return (IChildProjection)miBuildChildPrivate.GenericInvoke(type.GetGenericArguments(), null, new object[] { proj });
        }

        static MethodInfo miBuildChildPrivate = ReflectionTools.GetMethodInfo(() => BuildChildPrivate<int, bool>(null)).GetGenericMethodDefinition();


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
            ParameterExpression row = Expression.Parameter(typeof(IProjectionRow), "row");
            Scope scope; 

            public PropertyInfo piToStrLite = ReflectionTools.GetPropertyInfo((Lite l) =>l.ToStr);

            static MethodInfo miGetList = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.GetList<int>(null, 1)).GetGenericMethodDefinition();

            static MethodInfo miGetIdentifiable = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.GetIdentifiable<TypeDN>(null)).GetGenericMethodDefinition();
            static MethodInfo miGetImplementedBy = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.GetImplementedBy<TypeDN>(null, null)).GetGenericMethodDefinition(); 
            static MethodInfo miGetImplementedByAll = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.GetImplementedByAll<TypeDN>(null, null)).GetGenericMethodDefinition();

            static MethodInfo miGetLiteIdentifiable = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.GetLiteIdentifiable<TypeDN>(null, null, null)).GetGenericMethodDefinition(); 
            static MethodInfo miGetLiteImplementedByAll = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.GetLiteImplementedByAll<TypeDN>(null, null)).GetGenericMethodDefinition(); 

            static internal Expression<Func<IProjectionRow, T>> Build<T>(Expression expression, Scope scope)
            {
                ProjectionBuilder pb = new ProjectionBuilder() { scope = scope };
                Expression body = pb.Visit(expression);
                return Expression.Lambda<Func<IProjectionRow, T>>(body, pb.row);
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
                return Expression.Call(row, miGetIdentifiable.MakeGenericMethod(fieldInit.Type), 
                    Visit(NullifyColumn(fieldInit.ExternalId)));
            }

            protected override Expression VisitEmbeddedFieldInit(EmbeddedFieldInitExpression efie)
            {
                Expression ctor = Expression.MemberInit(Expression.New(efie.Type),
                       efie.Bindings.Select(b => Expression.Bind(b.FieldInfo, Visit(b.Binding))).ToArray());

                if (efie.HasValue == null)
                    return ctor;

                return Expression.Condition(Expression.Equal(Visit(efie.HasValue), Expression.Constant(true)), ctor, Expression.Constant(null, ctor.Type));
            }

            protected override Expression VisitMList(MListExpression ml)
            {
                return Expression.Call(row, miGetList.MakeGenericMethod(ml.Type.ElementType()),
                    Expression.Constant(ml.RelationalTable),
                    Visit(ml.BackID));
            }

            protected override Expression VisitImplementedBy(ImplementedByExpression rb)
            {
                Type[] types = rb.Implementations.Select(a => a.Type).ToArray();
                return Expression.Call(row,
                    miGetImplementedBy.MakeGenericMethod(rb.Type),
                    Expression.Constant(types),
                    Expression.NewArrayInit(typeof(int?),
                    rb.Implementations.Select(i => Visit(NullifyColumn(i.Field.ExternalId))).ToArray()));
            }

            protected override Expression VisitImplementedByAll(ImplementedByAllExpression rba)
            {
                return Expression.Call(row, miGetImplementedByAll.MakeGenericMethod(rba.Type),
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
                    return Expression.Call(row, miGetLiteImplementedByAll.MakeGenericMethod(liteType), id, typeId.Nullify());
                }
                else
                    return Expression.Call(row, miGetLiteIdentifiable.MakeGenericMethod(liteType), id, typeId, toStr);
            }

            protected override Expression VisitSqlConstant(SqlConstantExpression sce)
            {
                return Expression.Constant(sce.Value, sce.Type);
            }
        }
    }

    internal class Scope
    {
        public string Alias;

        public Dictionary<string, int> Positions;

        static PropertyInfo miReader = ReflectionTools.GetPropertyInfo((IProjectionRow row) => row.Reader);
 
        public Expression GetColumnExpression(Expression row, string alias, string name, Type type)
        {
            if (alias != Alias)
                    throw new InvalidOperationException("alias '{0}' not found".Formato(alias));

            return FieldReader.GetExpression(
                Expression.Property(row, miReader), Expression.Constant(Positions.GetOrThrow(name, "column name '{0}' not found in alias '" + alias + "'")), type);
        }

        static MethodInfo miLookup = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.Lookup<int, double>(null, 0)).GetGenericMethodDefinition();
        
        public Expression Lookup(Expression row, ChildProjectionExpression cProj)
        {
            Type t = cProj.Projection.UniqueFunction == null ? cProj.Type.ElementType() : cProj.Type;

            MethodInfo mi = miLookup.MakeGenericMethod(cProj.OuterKey.Type, t);

            Expression call = Expression.Call(row, mi, Expression.Constant(cProj.Projection.Token), cProj.OuterKey);

            if (cProj.Projection.UniqueFunction == null)
                return call;

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
