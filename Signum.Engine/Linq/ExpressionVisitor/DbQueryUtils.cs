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
            switch (e.NodeType)
            {
                case ExpressionType.Convert: return ((UnaryExpression)e).Operand.IsNull();
                case ExpressionType.Constant: return ((ConstantExpression)e).Value == null;
                default:
                    if (e is SqlConstantExpression)
                        return ((SqlConstantExpression)e).Value == null;
                    break;
            }

            return false;
        }
    }
}
