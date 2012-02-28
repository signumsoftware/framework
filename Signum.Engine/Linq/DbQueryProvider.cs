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
            using (Alias.NewGenerator())
            {
                ITranslateResult result;

                using (HeavyProfiler.Log("LINQ", () => expression.NiceToString()))
                using (var log = HeavyProfiler.LogNoStackTrace("Clean"))
                {
                    Expression cleaned = Clean(expression, true, log);
                    BinderTools tools = new BinderTools();
                    log.Switch("Bind");
                    ProjectionExpression binded = (ProjectionExpression)QueryBinder.Bind(cleaned, tools);
                    ProjectionExpression optimized = (ProjectionExpression)Optimize(binded, tools, log);
                    log.Switch("ChPrjFlatt");
                    ProjectionExpression flat = ChildProjectionFlattener.Flatten(optimized);
                    log.Switch("TB");
                    result = TranslatorBuilder.Build(flat);
                }
                return continuation(result);
            }
        }

        public static Expression Clean(Expression expression, bool filter, HeavyProfiler.Tracer log)
        {
            Expression clean = ExpressionCleaner.Clean(expression);
            log.Switch("OvrLdSmp");
            Expression simplified = OverloadingSimplifier.Simplify(clean);
            log.Switch("QrFlr");
            Expression filtered = QueryFilterer.Filter(simplified, filter);
            return filtered;
        }

        internal static Expression Optimize(Expression binded, BinderTools tools, HeavyProfiler.Tracer log)
        {
            log.Switch("AggRew");
            Expression rewrited = AggregateRewriter.Rewrite(binded);
            log.Switch("EnCom");
            Expression completed = EntityCompleter.Complete(rewrited, tools);
            log.Switch("OrBtRw");
            Expression orderRewrited = OrderByRewriter.Rewrite(completed);
            log.Switch("QuRb");
            Expression rebinded = QueryRebinder.Rebind(orderRewrited);
            log.Switch("AlPrRe");
            Expression replaced = AliasProjectionReplacer.Replace(rebinded);
            log.Switch("UnClRmv");
            Expression columnCleaned = UnusedColumnRemover.Remove(replaced);
            log.Switch("RwNmbFlr");
            Expression rowFilled = RowNumberFiller.Fill(columnCleaned);
            log.Switch("RdnSqRm");
            Expression subqueryCleaned = RedundantSubqueryRemover.Remove(rowFilled);
            log.Switch("CndRwr");
            Expression rewriteConditions = ConditionsRewriter.Rewrite(subqueryCleaned);
            return rewriteConditions;
        }

        internal int Delete(IQueryable query)
        {
            using (Alias.NewGenerator())
            {
                CommandResult cr;
                using (HeavyProfiler.Log("LINQ"))
                using (var log = HeavyProfiler.LogNoStackTrace("Clean"))
                {
                    Expression cleaned = Clean(query.Expression, true, log);
                    
                    BinderTools tools = new BinderTools();
                    log.Switch("Bind");
                    CommandExpression delete = new QueryBinder(tools).BindDelete(cleaned);
                    CommandExpression deleteOptimized = (CommandExpression)Optimize(delete, tools, log);
                    cr = TranslatorBuilder.BuildCommandResult(deleteOptimized);
                }
                return cr.Execute();
            }
        }

        internal int Update<T>(IQueryable<T> query, Expression<Func<T, T>> set)
        {
            using (Alias.NewGenerator())
            {
                CommandResult cr;
                using (HeavyProfiler.Log("LINQ"))
                using (var log = HeavyProfiler.LogNoStackTrace("Clean"))
                {
                    Expression cleaned = Clean(query.Expression, true, log);
                    BinderTools tools = new BinderTools();
                    log.Switch("Bind");
                    CommandExpression update = new QueryBinder(tools).BindUpdate(cleaned, set);
                    CommandExpression updateOptimized = (CommandExpression)Optimize(update, tools, log);
                    log.Switch("TR");
                    cr = TranslatorBuilder.BuildCommandResult(updateOptimized);
                }
                return cr.Execute();
            }
        }
    }
}
