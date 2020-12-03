using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Signum.Engine.Linq
{
    /// <summary>
    /// A visitor that replaces references to one specific instance of a node with another
    /// </summary>
    internal class Replacer : DbExpressionVisitor
    {
        Expression searchFor;
        Expression replaceWith;

        private Replacer(Expression searchFor, Expression replaceWith)
        {
            this.searchFor = searchFor;
            this.replaceWith = replaceWith;
        }

        static internal Expression Replace(Expression expression, Expression searchFor, Expression replaceWith)
        {
            return new Replacer(searchFor, replaceWith).Visit(expression);
        }

        [return: NotNullIfNotNull("exp")]
        public override Expression? Visit(Expression? exp)
        {
            if (exp != null && (exp == this.searchFor || exp.Equals(this.searchFor)))
            {
                return this.replaceWith;
            }
            return base.Visit(exp);
        }
    }
}
