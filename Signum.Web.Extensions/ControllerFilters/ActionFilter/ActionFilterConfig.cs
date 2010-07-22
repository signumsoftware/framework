using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Linq.Expressions;

namespace Signum.Web
{
    public class ActionFilterConfig<T> : IActionFilterConfig where T : Controller
    {
        Dictionary<string, IList<FilterAttribute>> _actionFiterAddedConfig =
                                           new Dictionary<string, IList<FilterAttribute>>();

        IList<FilterAttribute> _actionFiterAddedToController;

        public ActionFilterConfig<T> AddFilterToAction(Expression<Func<T, ActionResult>> expression,
                                                       params FilterAttribute[] actionFilters)
        {
            if (actionFilters == null || actionFilters.Length == 0)
                throw new ArgumentNullException("actionFilters");

            var methodCallEpxression = expression.Body as MethodCallExpression;
            var actionMethodName = methodCallEpxression.Method.Name;

            if (_actionFiterAddedConfig.ContainsKey(actionMethodName))
            {
                foreach (var actionFilter in actionFilters)
                    _actionFiterAddedConfig[actionMethodName].Add(actionFilter);
            }
            else
                _actionFiterAddedConfig.Add(actionMethodName, actionFilters.ToList());

            return this;
        }


        public ActionFilterConfig<T> AddFilterToActions(FilterAttribute actionFilter,
                                                       params Expression<Func<T, ActionResult>>[] expressions)
        {
            if (expressions == null || expressions.Length == 0)
                throw new ArgumentNullException("expression");

            if (actionFilter == null)
                throw new ArgumentNullException("actionFilter");

            foreach (var expression in expressions)
            {
                var methodCallEpxression = expression.Body as MethodCallExpression;
                var actionMethodName = methodCallEpxression.Method.Name;

                if (!_actionFiterAddedConfig.ContainsKey(actionMethodName))
                    _actionFiterAddedConfig.Add(actionMethodName, new List<FilterAttribute>());

                _actionFiterAddedConfig[actionMethodName].Add(actionFilter);
            }

            return this;
        }


        public ActionFilterConfig<T> AddFilterToController(params FilterAttribute[] actionFilters)
        {
             if (actionFilters == null || actionFilters.Length == 0)
                throw new ArgumentNullException("actionFilters");

             if (_actionFiterAddedToController == null)
                 _actionFiterAddedToController = new List<FilterAttribute>();

             foreach (FilterAttribute fa in actionFilters)
                 _actionFiterAddedToController.Add(fa);
            return this;
        }


        public Dictionary<string, IList<FilterAttribute>> ActionFilterAddedToActions
        {
            get { return _actionFiterAddedConfig; }
        }


        public IEnumerable<FilterAttribute> ActionFilterAddedToController
        {
            get { return _actionFiterAddedToController; }
        }
    }
}
