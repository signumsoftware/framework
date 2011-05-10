using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Data.SqlClient;
using System.Diagnostics;
using Signum.Utilities.Reflection;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine;
using System.Data;
using Signum.Entities;

namespace Signum.Engine.Linq
{

    /// <summary>
    /// Stateless query provider 
    /// </summary>
    public class DbQueryProvider : QueryProvider
    {
        public static readonly DbQueryProvider Single = new DbQueryProvider();

        private DbQueryProvider()
        {
        }
    
        public override string GetQueryText(Expression expression)
        {
            return this.Translate(expression, tr => tr.CleanCommandText());
        }
        
        public override object Execute(Expression expression)
        {
            using (HeavyProfiler.Log("DB"))
                return this.Translate(expression, tr => tr.Execute());
        }

        T Translate<T>(Expression expression, Func<ITranslateResult, T> continuation) //For debugging purposes
        {
            Expression cleaned = Clean(expression);
            Expression filtered = QueryFilterer.Filter(cleaned);

            BinderTools tools = new BinderTools();

            ProjectionExpression binded = (ProjectionExpression)QueryBinder.Bind(filtered, tools);
            ProjectionExpression optimized = (ProjectionExpression)Optimize(binded, tools);

            ProjectionExpression flat = ChildProjectionFlattener.Flatten(optimized, tools.AliasGenerator);

            ITranslateResult result = TranslatorBuilder.Build(flat);
            return continuation(result);
        }

        public static Expression Clean(Expression expression)
        {
            Expression clean = ExpressionCleaner.Clean(expression);
            Expression simplified = OverloadingSimplifier.Simplify(clean);
            return simplified;
        }

        internal static Expression Optimize(Expression binded, BinderTools tools)
        {
            Expression rewrited = AggregateRewriter.Rewrite(binded);
            Expression completed = EntityCompleter.Complete(rewrited, tools);
            Expression orderRewrited = OrderByRewriter.Rewrite(completed);

            Expression rebinded = QueryRebinder.Rebind(orderRewrited);

            Expression replaced = AliasProjectionReplacer.Replace(rebinded, tools.AliasGenerator);
            Expression columnCleaned = UnusedColumnRemover.Remove(replaced);
            Expression rowFilled = RowNumberFiller.Fill(columnCleaned);
            Expression subqueryCleaned = RedundantSubqueryRemover.Remove(rowFilled);

            Expression rewriteConditions = ConditionsRewriter.Rewrite(subqueryCleaned);
            return rewriteConditions;
        }

        internal int Delete(IQueryable query)
        {
            Expression cleaned = Clean(query.Expression);
            Expression filtered = QueryFilterer.Filter(cleaned);
            BinderTools tools = new BinderTools();
            CommandExpression delete = new QueryBinder(tools).BindDelete(filtered);
            CommandExpression deleteOptimized = (CommandExpression)Optimize(delete, tools);
            CommandResult cr = TranslatorBuilder.BuildCommandResult(deleteOptimized);

            return cr.Execute();
        }

        internal int Update<T>(IQueryable<T> query, Expression<Func<T, T>> set)
        {
            Expression cleaned = Clean(query.Expression);
            Expression filtered = QueryFilterer.Filter(cleaned);

            BinderTools tools = new BinderTools();
            CommandExpression update = new QueryBinder(tools).BindUpdate(filtered, set);
            CommandExpression updateOptimized = (CommandExpression)Optimize(update, tools);
            CommandResult cr = TranslatorBuilder.BuildCommandResult(updateOptimized);

            return cr.Execute();
        }
    }
}
