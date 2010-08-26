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
        static internal ITranslateResult Build(ProjectionExpression proj, Scope previousScope)
        {
            Type type = proj.UniqueFunction == null ? proj.Type.ElementType() : proj.Type;

            return (ITranslateResult)miBuildPrivate.GenericInvoke(new[] { type }, null, new object[] { proj, previousScope });
        }

        static MethodInfo miBuildPrivate = ReflectionTools.GetMethodInfo(() => BuildTranslateResult<int>(null, null)).GetGenericMethodDefinition();

        static internal TranslateResult<T> BuildTranslateResult<T>(ProjectionExpression proj, Scope previousScope)
        {
            Scope scope = new Scope
            {
                Alias = proj.Source.Alias,
                Parent = previousScope,
                Positions = proj.Source.Columns.Select((c, i) => new { c.Name, i }).ToDictionary(p => p.Name, p => p.i)
            };


            Expression<Func<IProjectionRow, T>> lambda = ProjectionBuilder.Build<T>(proj.Projector, scope);

            Expression<Func<IProjectionRow, SqlParameter[]>> createParams;
            string sql = QueryFormatter.Format(proj.Source, previousScope, out createParams);

            var result = new TranslateResult<T>
            {
                CommandText = sql,

                ProjectorExpression = lambda,

                GetParameters = createParams.Compile(),
                GetParametersExpression = createParams,

                Unique = proj.UniqueFunction,
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

            protected override Expression VisitColumn(ColumnExpression column)
            {
                return scope.GetColumnExpression(row, column.Alias, column.Name, column.Type);
            }

            MethodInfo miExecute = ReflectionTools.GetMethodInfo((ITranslateResult it) => it.Execute(null));

            protected override Expression VisitProjection(ProjectionExpression proj)
            {
                ITranslateResult tr = TranslatorBuilder.Build(proj, scope);

                Expression call = Expression.Call(Expression.Constant(tr), miExecute, this.row);

                if (typeof(IEnumerable).IsAssignableFrom(proj.Type))
                    return Expression.Convert(call, typeof(IEnumerable<>).MakeGenericType(proj.Type.ElementType()));
                else
                    return call;
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
        public Scope Parent;

        public string Alias;

        public Dictionary<string, int> Positions = new Dictionary<string, int>();

        static PropertyInfo miParent = ReflectionTools.GetPropertyInfo((IProjectionRow row) => row.Parent);
        static PropertyInfo miReader = ReflectionTools.GetPropertyInfo((IProjectionRow row) => row.Reader);

        public Expression GetColumnExpression(Expression row, string alias, string name, Type type)
        {
            if (alias != Alias)
            {
                if (Parent == null)
                    throw new InvalidOperationException("alias '{0}' not found".Formato(alias));

                return Parent.GetColumnExpression(Expression.Property(row, miParent), alias, name, type);
            }

            return FieldReader.GetExpression(
                Expression.Property(row, miReader), Expression.Constant(Positions.GetOrThrow(name, "column name '{0'} not found in alias '" + alias + "'")), type);
        }

        internal bool ContainAlias(string alias)
        {
            return alias == Alias || (Parent != null && Parent.ContainAlias(alias));
        }
    }
}
