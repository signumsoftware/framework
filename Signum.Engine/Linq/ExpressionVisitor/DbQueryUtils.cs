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
        internal static bool IsNull(this Expression e)
        {
            ConstantExpression ce = e as ConstantExpression;
            SqlConstantExpression sce = e as SqlConstantExpression;
            return ce != null && ce.Value == null || 
                sce != null && sce.Value == null;
        }
    }
}
