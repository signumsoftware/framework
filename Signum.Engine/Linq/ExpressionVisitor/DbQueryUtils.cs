using System.Linq.Expressions;

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
                    if (e is SqlConstantExpression sce)
                        return sce.Value == null;
                    break;
            }

            return false;
        }
    }
}
