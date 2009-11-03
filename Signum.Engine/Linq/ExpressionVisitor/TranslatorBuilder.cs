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
        static internal ITranslateResult Build(ProjectionExpression proj, ImmutableStack<string> prevAliases)
        {
            try
            {
                Type type =
                    proj.UniqueFunction == null ? ReflectionTools.CollectionType(proj.Type) : proj.Type;

                return (ITranslateResult)miBuildPrivate.MakeGenericMethod(type).Invoke(null, new object[] { proj, prevAliases });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        static MethodInfo miBuildPrivate = ReflectionTools.GetMethodInfo(() => BuildTranslateResult<int>(null, null)).GetGenericMethodDefinition();

        static internal TranslateResult<T> BuildTranslateResult<T>(ProjectionExpression proj, ImmutableStack<string> prevAliases)
        {
            string alias = proj.Source.Alias;

            var aliases = prevAliases.Push(alias); 

            bool hasFullObjects; 

            Expression<Func<IProjectionRow, T>> lambda = ProjectionBuilder.Build<T>(proj.Projector, aliases, out hasFullObjects);

            Expression<Func<IProjectionRow, SqlParameter[]>> createParams;
            string sql = QueryFormatter.Format(proj.Source, aliases, out createParams);

            var result = new TranslateResult<T>
            {
                Alias = alias,
                CommandText = sql,

                ProjectorExpression = lambda,

                GetParameters = createParams.Compile(),
                GetParametersExpression = createParams,

                Unique = proj.UniqueFunction,

                HasFullObjects = hasFullObjects,
            };

            return result;
        }

        public static CommandResult BuildCommandResult<T>(CommandExpression command)
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

        /// <summary>
        /// ProjectionBuilder is a visitor that converts an projector expression
        /// that constructs result objects out of ColumnExpressions into an actual
        /// LambdaExpression that constructs result objects out of accessing fields
        /// of a ProjectionRow
        /// </summary>
        public class ProjectionBuilder : DbExpressionVisitor
        {
            ParameterExpression row = Expression.Parameter(typeof(IProjectionRow), "row");
            ImmutableStack<string> prevAliases;

            public PropertyInfo piToStrLite = ReflectionTools.GetPropertyInfo((Lite l) =>l.ToStr);

            static MethodInfo miGetValue = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.GetValue<int>(null, null)).GetGenericMethodDefinition();

            bool HasFullObjects;

            static MethodInfo miGetList = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.GetList<int>(null, 1)).GetGenericMethodDefinition();

            static MethodInfo miGetIdentifiable = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.GetIdentifiable<TypeDN>(null)).GetGenericMethodDefinition();
            static MethodInfo miGetImplementedBy = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.GetImplementedBy<TypeDN>(null, null)).GetGenericMethodDefinition(); 
            static MethodInfo miGetImplementedByAll = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.GetImplementedByAll<TypeDN>(null, null)).GetGenericMethodDefinition();

            static MethodInfo miGetLiteIdentifiable = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.GetLiteIdentifiable<TypeDN>(null, null, null)).GetGenericMethodDefinition(); 
            static MethodInfo miGetLiteImplementedByAll = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.GetLiteImplementedByAll<TypeDN>(null, null)).GetGenericMethodDefinition(); 

            static internal Expression<Func<IProjectionRow, T>> Build<T>(Expression expression, ImmutableStack<string> prevAliases, out bool hasFullObjects)
            {
                ProjectionBuilder pb = new ProjectionBuilder() { prevAliases = prevAliases };
                Expression body = pb.Visit(expression);
                hasFullObjects = pb.HasFullObjects;
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

            protected override Expression VisitColumn(ColumnExpression column)
            {
                Debug.Assert(prevAliases.Contains(column.Alias));

                return Expression.Call(this.row,
                    miGetValue.MakeGenericMethod(column.Type),
                    Expression.Constant(column.Alias),
                    Expression.Constant(column.Name));
            }

            MethodInfo miExecute = ReflectionTools.GetMethodInfo((ITranslateResult it) => it.Execute(null));

            protected override Expression VisitProjection(ProjectionExpression proj)
            {
                ITranslateResult tr = TranslatorBuilder.Build(proj, prevAliases);
                HasFullObjects |= tr.HasFullObjects;

                Expression call = Expression.Call(Expression.Constant(tr), miExecute, this.row);

                if (typeof(IEnumerable).IsAssignableFrom(proj.Type))
                    return Expression.Convert(call, typeof(IEnumerable<>).MakeGenericType(ReflectionTools.CollectionType(proj.Type)));
                else
                    return call;
            }

            protected override Expression VisitFieldInit(FieldInitExpression fieldInit)
            {
                HasFullObjects = true;
                return Expression.Call(row, miGetIdentifiable.MakeGenericMethod(fieldInit.Type), 
                    Visit(NullifyColumn(fieldInit.ExternalId)));
            }

            protected override Expression VisitEmbeddedFieldInit(EmbeddedFieldInitExpression efie)
            {
                return Expression.MemberInit(Expression.New(efie.Type),
                       efie.Bindings.Select(b => Expression.Bind(b.FieldInfo, Visit(b.Binding))).ToArray());
            }

            protected override Expression VisitMList(MListExpression ml)
            {
                HasFullObjects = true;
                return Expression.Call(row, miGetList.MakeGenericMethod(ReflectionTools.CollectionType(ml.Type)),
                    Expression.Constant(ml.RelationalTable),
                    Visit(ml.BackID));
            }

            protected override Expression VisitImplementedBy(ImplementedByExpression rb)
            {
                HasFullObjects = true;
                Type[] types = rb.Implementations.Select(a => a.Type).ToArray();
                return Expression.Call(row,
                    miGetImplementedBy.MakeGenericMethod(rb.Type),
                    Expression.Constant(types),
                    Expression.NewArrayInit(typeof(int?),
                    rb.Implementations.Select(i => Visit(NullifyColumn(i.Field.ExternalId))).ToArray()));
            }

            protected override Expression VisitImplementedByAll(ImplementedByAllExpression rba)
            {
                HasFullObjects = true;
                return Expression.Call(row, miGetImplementedByAll.MakeGenericMethod(rba.Type),
                    Visit(NullifyColumn(rba.Id)),
                    Visit(NullifyColumn(rba.TypeId)));
            }

            protected override Expression VisitLiteReference(LiteReferenceExpression lite)
            {
                var id = Visit(NullifyColumn(lite.Id));
                var toStr = Visit(lite.ToStr);
                var typeId = Visit(lite.TypeId);

                Type liteType = Reflector.ExtractLite(lite.Type);

                if (id == null)
                    return Expression.Constant(null, lite.Type);
                else if (toStr == null)
                {
                    HasFullObjects = true;
                    return Expression.Call(row, miGetLiteImplementedByAll.MakeGenericMethod(liteType), id, typeId.Nullify());
                }
                else
                    return Expression.Call(row, miGetLiteIdentifiable.MakeGenericMethod(liteType), id, typeId, toStr);
            }
        }
    }
}
