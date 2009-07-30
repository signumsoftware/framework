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

namespace Signum.Engine.Linq
{  
    internal static class TranslatorBuilder
    {
        static internal ITranslateResult Build(ProjectionExpression proj, ImmutableStack<string> prevAliases)
        {
            try
            {
                return (ITranslateResult)mi.MakeGenericMethod(proj.Projector.Type).Invoke(null, new object[] { proj, prevAliases });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        static MethodInfo mi = typeof(TranslatorBuilder).GetMethod("BuildPrivate", BindingFlags.NonPublic | BindingFlags.Static);

        static internal TranslateResult<T> BuildPrivate<T>(ProjectionExpression proj, ImmutableStack<string> prevAliases)
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

        /// <summary>
        /// ProjectionBuilder is a visitor that converts an projector expression
        /// that constructs result objects out of ColumnExpressions into an actual
        /// LambdaExpression that constructs result objects out of accessing fields
        /// of a ProjectionRow
        /// </summary>
        public class ProjectionBuilder:DbExpressionVisitor
        {
            ParameterExpression row = Expression.Parameter(typeof(IProjectionRow), "row");
            ImmutableStack<string> prevAliases;

            public PropertyInfo piToStrLazy = ReflectionTools.GetPropertyInfo<Lazy>(l => l.ToStr);

            static MethodInfo miGetValue = typeof(IProjectionRow).GetMethod("GetValue");
            //static MethodInfo miExecuteSubQuery = typeof(IProjectionRow).GetMethod("ExecuteSubQuery");

            bool HasFullObjects;

            static MethodInfo miGetList = typeof(IProjectionRow).GetMethod("GetList");

            static MethodInfo miGetIdentificable = typeof(IProjectionRow).GetMethod("GetIdentificable");
            static MethodInfo miGetImplementedBy = typeof(IProjectionRow).GetMethod("GetImplementedBy");
            static MethodInfo miGetImplementedByAll = typeof(IProjectionRow).GetMethod("GetImplementedByAll");

            static MethodInfo miGetLazyIdentificable = typeof(IProjectionRow).GetMethod("GetLazyIdentificable");
            static MethodInfo miGetLazyImplementedBy = typeof(IProjectionRow).GetMethod("GetLazyImplementedBy");
            static MethodInfo miGetLazyImplementedByAll = typeof(IProjectionRow).GetMethod("GetLazyImplementedByAll");

            static internal Expression<Func<IProjectionRow, T>> Build<T>(Expression expression, ImmutableStack<string> prevAliases, out bool hasFullObjects)
            {
                ProjectionBuilder pb = new ProjectionBuilder() { prevAliases = prevAliases };
                Expression body = pb.Visit(expression);
                hasFullObjects = pb.HasFullObjects;
                return Expression.Lambda<Func<IProjectionRow, T>>(body, pb.row);
            }

            ColumnExpression NullifyColumn(Expression exp)
            {
                ColumnExpression ce = (ColumnExpression)exp;
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

            MethodInfo mi = ReflectionTools.GetMethodInfo<ITranslateResult>(it => it.Execute(null));

            protected override Expression VisitProjection(ProjectionExpression proj)
            {
                ITranslateResult tr = TranslatorBuilder.Build(proj, prevAliases);
                HasFullObjects |= tr.HasFullObjects;
                return Expression.Convert(Expression.Call(Expression.Constant(tr), mi, this.row), proj.Type);
            }

            protected override Expression VisitLazyLiteral(LazyLiteralExpression lazy)
            {
                var id = Visit(NullifyColumn( lazy.ID));
                var toStr= Visit(lazy.ToStr);

                Type lazyType = Reflector.ExtractLazy(lazy.Type);

                return Expression.Call(row, miGetLazyIdentificable.MakeGenericMethod(lazyType),
                    Expression.Constant(lazy.RuntimeType), id, toStr);
            }

            protected override Expression VisitFieldInit(FieldInitExpression fieldInit)
            {
                if (typeof(IdentifiableEntity).IsAssignableFrom(fieldInit.Type))
                {
                    HasFullObjects = true;
                    return Expression.Call(row, miGetIdentificable.MakeGenericMethod(fieldInit.Type), Visit(NullifyColumn(fieldInit.ExternalId)));
                }
                else
                    return Expression.MemberInit(Expression.New(fieldInit.Type),
                        fieldInit.Bindings.Select(b => Expression.Bind(b.FieldInfo, Visit(b.Binding))).ToArray());
            }

            protected override Expression VisitMList(MListExpression ml)
            {
                HasFullObjects = true;
                return Expression.Call(row, miGetList.MakeGenericMethod(ReflectionTools.CollectionType(ml.Type)),
                    Expression.Constant(ml.RelationalTable),
                    Visit(ml.BackID));
            }

            //protected override Expression VisitEnumExpression(EnumExpression enumExp)
            //{
            //    return Expression.Convert(Visit(enumExp.ID), enumExp.Type);
            //}

            protected override Expression VisitImplementedBy(ImplementedByExpression rb)
            {
                HasFullObjects = true; 
                Type[] types = rb.Implementations.Select(a => a.Type).ToArray();
                return Expression.Call(row, miGetImplementedBy.MakeGenericMethod(rb.Type),
                    Expression.Constant(types),
                    Expression.NewArrayInit(typeof(int?), rb.Implementations.Select(i => Visit(NullifyColumn(i.Field.ExternalId))).ToArray()));
            }

            protected override Expression VisitImplementedByAll(ImplementedByAllExpression rba)
            {
                HasFullObjects = true;
                return Expression.Call(row, miGetImplementedByAll.MakeGenericMethod(rba.Type),
                    Visit(NullifyColumn(rba.ID)),
                    Visit(NullifyColumn(rba.TypeID)));
            }
            
            protected override Expression VisitLazyReference(LazyReferenceExpression lazy)
            {
                HasFullObjects = true;
                Type lazyType = Reflector.ExtractLazy(lazy.Type);
                Expression reference = lazy.Reference;
                switch ((DbExpressionType)reference.NodeType)
                {
                    case DbExpressionType.FieldInit:
                        Debug.Assert(false);
                        return Expression.Call(row, miGetLazyIdentificable.MakeGenericMethod(lazyType), Expression.Constant(reference.Type),
                            Visit(NullifyColumn(((FieldInitExpression)reference).ExternalId)));
                    case DbExpressionType.ImplementedBy:
                        ImplementedByExpression rb = (ImplementedByExpression)reference;
                        Type[] types = rb.Implementations.Select(a => a.Type).ToArray();
                        return Expression.Call(row, miGetLazyImplementedBy.MakeGenericMethod(lazyType),
                            Expression.Constant(types),
                            Expression.NewArrayInit(typeof(int?), rb.Implementations.Select(i => Visit(NullifyColumn( i.Field.ExternalId))).ToArray()));
                    case DbExpressionType.ImplementedByAll:
                        ImplementedByAllExpression rba = (ImplementedByAllExpression)reference;
                        return Expression.Call(row, miGetLazyImplementedByAll.MakeGenericMethod(lazyType),
                            Visit(NullifyColumn(rba.ID)),
                            Visit(NullifyColumn(rba.TypeID)).Nullify());
                    default:
                        throw new NotSupportedException();
                }
            }

        }
    }
}
