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
    internal class DbQueryProvider : QueryProvider
    {       
        public static readonly DbQueryProvider Single = new DbQueryProvider();

        private DbQueryProvider()
        {
        }
    
        public override string GetQueryText(Expression expression)
        {
            return this.Translate(expression).CommandText;
        }
        
        public override object Execute(Expression expression)
        {
            ITranslateResult tr = this.Translate(expression);

            return tr.Execute(null);
        }
     
        private ITranslateResult Translate(Expression expression)
        {
            Expression expand = ExpressionExpander.ExpandUntyped(expression);
            Expression partialEval = ExpressionEvaluator.PartialEval(expand);
            Expression simplified = OverloadingSimplifier.Simplify(partialEval);
            ProjectionExpression binded = (ProjectionExpression)QueryBinder.Bind(simplified);
            ProjectionExpression rewrited = (ProjectionExpression)AggregateRewriter.Rewrite(binded);
            ProjectionExpression rebinded = (ProjectionExpression)QueryRebinder.Rebind(rewrited);
            ProjectionExpression projCleaned = (ProjectionExpression)ProjectionCleaner.Clean(rebinded);
            ProjectionExpression ordered = OrderByRewriter.Rewrite(projCleaned);
            ProjectionExpression replaced = AliasProjectionReplacer.Replace(ordered);
            ProjectionExpression columnCleaned = (ProjectionExpression)UnusedColumnRemover.Remove(replaced);
            ProjectionExpression subqueryCleaned = (ProjectionExpression)RedundantSubqueryRemover.Remove(columnCleaned);

            ITranslateResult result = TranslatorBuilder.Build((ProjectionExpression)subqueryCleaned, ImmutableStack<string>.Empty);
            return result; 
        }
    }
}
