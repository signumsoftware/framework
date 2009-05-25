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
            return this.Translate(expression).CommandText;
        }

        MethodInfo mi = typeof(DbQueryProvider).GetMethod("ExecutePrivateUnique", BindingFlags.NonPublic | BindingFlags.Instance);


        public override object ExecutePrivate<S>(Expression expression)
        {
            ITranslateResult tr = this.Translate(expression);

            if (tr.UniqueFunction == UniqueFunction.SingleIsZero || tr.UniqueFunction == UniqueFunction.SingleGreaterThanZero)
            {
                IEnumerable<int> reader = ExecuteReader<int>((TranslateResult<int>)tr, null);
                int count = reader.Single();
                if (tr.UniqueFunction == UniqueFunction.SingleGreaterThanZero)
                    return count > 0;
                else
                    return count == 0; 
            }
            else
            {
                IEnumerable<S> reader = ExecuteReader<S>((TranslateResult<S>)tr, null);

                if (tr.UniqueFunction.HasValue)
                    switch (tr.UniqueFunction.Value)
                    {
                        case UniqueFunction.First: return reader.First();
                        case UniqueFunction.FirstOrDefault: return reader.FirstOrDefault();
                        case UniqueFunction.Single: return reader.Single();
                        case UniqueFunction.SingleOrDefault: return reader.SingleOrDefault();
                        default:
                            throw new InvalidOperationException();
                    }
                else
                    if (tr.HasFullObjects)
                        return reader.ToList();
                    else
                        return reader;
            }
        }

        internal IEnumerable<T> ExecuteReader<T>(TranslateResult<T> tr, IProjectionRow pr)
        {
            SqlPreCommandSimple command = new SqlPreCommandSimple(tr.CommandText, tr.GetParameters(pr).ToList());

            DataTable dt = Executor.ExecuteDataTable(command); 

            ProjectionRowEnumerator<T> enumerator = new ProjectionRowEnumerator<T>(dt, tr.ProjectorExpression, this, tr.HasFullObjects, pr, tr.Alias);

            return new ProjectionRowReader<T>(enumerator).ToList();
        }
     
        private ITranslateResult Translate(Expression expression)
        {
            Expression partialEval = Evaluator.PartialEval(expression);
            Expression simplified = OverloadingSimplifier.Simplify(partialEval);
            ProjectionExpression binded = (ProjectionExpression)QueryBinder.Bind(simplified);
            ProjectionExpression projCleaned = (ProjectionExpression)ProjectionCleaner.Clean(binded);
            ProjectionExpression ordered = OrderByRewriter.Rewrite(projCleaned);
            ProjectionExpression aggregate = AggregateOptimizer.Optimize(ordered);
            ProjectionExpression replaced = AliasProjectionReplacer.Replace(aggregate);
            ProjectionExpression columnCleaned = (ProjectionExpression)UnusedColumnRemover.Remove(replaced);
            ProjectionExpression optimized = SingleCellOptimizer.Optimize(columnCleaned);
            ProjectionExpression subqueryCleaned = (ProjectionExpression)RedundantSubqueryRemover.Remove(optimized);

            ITranslateResult result = TranslatorBuilder.Build((ProjectionExpression)subqueryCleaned, ImmutableStack<string>.Empty);

            return result; 
        }
    }
}
