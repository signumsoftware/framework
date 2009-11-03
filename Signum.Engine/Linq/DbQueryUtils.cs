using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Engine.Linq
{
    internal static class DbQueryUtils
    {
        internal static Expression Clean(Expression expression)
        {
            Expression expand = ExpressionExpander.ExpandUntyped(expression);
            Expression partialEval = ExpressionEvaluator.PartialEval(expand);
            Expression simplified = OverloadingSimplifier.Simplify(partialEval);
            return simplified;
        }

        internal static Expression Optimize(Expression binded)
        {
            Expression rewrited = AggregateRewriter.Rewrite(binded);
            Expression rebinded = QueryRebinder.Rebind(rewrited);
            Expression projCleaned = ProjectionCleaner.Clean(rebinded);
            Expression replaced = AliasProjectionReplacer.Replace(projCleaned);
            Expression columnCleaned = UnusedColumnRemover.Remove(replaced);
            Expression subqueryCleaned = RedundantSubqueryRemover.Remove(columnCleaned);
            Expression ordered = OrderByAsserter.Assert(subqueryCleaned);
            return ordered; 
        }

        internal static int Delete<T>(Expression<Func<T, bool>> predicate)
            where T:IdentifiableEntity
        {
            LambdaExpression cleanPredicate = predicate == null ? null : (LambdaExpression)Clean(predicate);
            CommandExpression delete = new QueryBinder().BindDelete<T>(cleanPredicate);
            CommandExpression deleteOptimized = (CommandExpression)Optimize(delete);
            CommandResult cr = TranslatorBuilder.BuildCommandResult<T>(deleteOptimized);

            return cr.Execute(); 
        }

        internal static int Update<T>(Expression<Func<T, T>> set, Expression<Func<T, bool>> predicate)
         where T : IdentifiableEntity
        {
            LambdaExpression cleanPredicate = predicate == null ? null : (LambdaExpression)Clean(predicate);
            CommandExpression update = new QueryBinder().BindUpdate<T>(set, cleanPredicate);
            CommandExpression updateOptimized = (CommandExpression)Optimize(update);
            CommandResult cr = TranslatorBuilder.BuildCommandResult<T>(updateOptimized);

            return cr.Execute();
        }

        internal static bool IsNull(this Expression e)
        {
            ConstantExpression ce = e as ConstantExpression;
            return ce != null && ce.Value == null;
        }
    }
}
