using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Maps;
using Signum.Entities;

namespace Signum.Engine.Linq
{
    public static class QueryUtils
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
            Expression ordered = OrderByRewriter.Rewrite(projCleaned);
            Expression replaced = AliasProjectionReplacer.Replace(ordered);
            Expression columnCleaned = UnusedColumnRemover.Remove(replaced);
            Expression subqueryCleaned = RedundantSubqueryRemover.Remove(columnCleaned);
            return subqueryCleaned; 
        }

        internal static int Delete<T>(Expression<Func<T, bool>> predicate)
            where T:IdentifiableEntity
        {
            LambdaExpression cleanPredicate = (LambdaExpression)Clean(predicate);

            DeleteExpression delete = new QueryBinder().BindDelete<T>(cleanPredicate);

            DeleteExpression deleteOptimized = (DeleteExpression)Optimize(delete);

            CommandResult cr = TranslatorBuilder.DeleteUpdate<T>(deleteOptimized);

            return cr.Execute(); 
        }

        internal static int Update<T>(Expression<Func<T, T>> set, Expression<Func<T, bool>> predicate)
         where T : IdentifiableEntity
        {
            LambdaExpression cleanPredicate = (LambdaExpression)Clean(predicate);
            LambdaExpression cleanSet = (LambdaExpression)Clean(set);

            UpdateExpression update = new QueryBinder().BindUpdate<T>(cleanPredicate, cleanSet);

            UpdateExpression updateOptimized = (UpdateExpression)Optimize(update);

            CommandResult cr = TranslatorBuilder.DeleteUpdate<T>(updateOptimized);

            return cr.Execute();
        }

      
    }
}
