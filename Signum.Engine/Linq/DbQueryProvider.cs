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
            ProjectionExpression binded = (ProjectionExpression)QueryBinder.Bind(filtered);
            ProjectionExpression optimized = (ProjectionExpression)Optimize(binded);

            ProjectionExpression flat = ChildProjectionFlattener.Flatten(optimized);

            ITranslateResult result = TranslatorBuilder.Build(flat);
            return continuation(result);
        }

        public static Expression Clean(Expression expression)
        {
            Expression clean = ExpressionCleaner.Clean(expression);
            Expression simplified = OverloadingSimplifier.Simplify(clean);
            return simplified;
        }

        internal static Expression Optimize(Expression binded)
        {
            Expression rewrited = AggregateRewriter.Rewrite(binded);
            Expression rebinded = QueryRebinder.Rebind(rewrited);
            Expression projCleaned = EntityCleaner.Clean(rebinded);
            Expression replaced = AliasProjectionReplacer.Replace(projCleaned);
            Expression removed = CountOrderByRemover.Remove(replaced);
            Expression columnCleaned = UnusedColumnRemover.Remove(removed);
            Expression rowFilled = RowNumberFiller.Fill(columnCleaned);
            Expression subqueryCleaned = RedundantSubqueryRemover.Remove(rowFilled);
            return subqueryCleaned;
        }

        internal int Delete<T>(IQueryable<T> query)
            where T : IdentifiableEntity
        {
            Expression cleaned = Clean(query.Expression);
            Expression filtered = QueryFilterer.Filter(cleaned);
            CommandExpression delete = new QueryBinder().BindDelete(filtered);
            CommandExpression deleteOptimized = (CommandExpression)Optimize(delete);
            CommandResult cr = TranslatorBuilder.BuildCommandResult(deleteOptimized);

            return cr.Execute();
        }

        internal int Update<T>(IQueryable<T> query, Expression<Func<T, T>> set)
            where T : IdentifiableEntity
        {
            Expression cleaned = Clean(query.Expression);
            Expression filtered = QueryFilterer.Filter(cleaned);
            CommandExpression update = new QueryBinder().BindUpdate(filtered, set);
            CommandExpression updateOptimized = (CommandExpression)Optimize(update);
            CommandResult cr = TranslatorBuilder.BuildCommandResult(updateOptimized);

            return cr.Execute();
        }
    }
}
