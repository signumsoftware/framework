using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Entities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using Signum.Entities.Reflection;

namespace Signum.Engine.Linq
{


    /// <summary>
    ///  returns the set of all aliases produced by a query source
    /// </summary>
    internal class BoolSimplifier : ExpressionVisitor
    {
        public static Expression Simplify(Expression expression)
        {
            return new BoolSimplifier().Visit(expression); 
        }

        bool GetVale(Expression exp)
        {
            return (bool)((ConstantExpression)exp).Value;
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            UnaryExpression ue = (UnaryExpression)base.VisitUnary(u);

            if (ue.Type != typeof(bool))
                return ue;

            if (ue.NodeType == ExpressionType.Not && ue.Operand.NodeType == ExpressionType.Constant)
                return Expression.Constant(!GetVale(ue.Operand));

            return ue;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            BinaryExpression be = (BinaryExpression)base.VisitBinary(b);

            if (be.Type != typeof(bool))
                return be;

            if (be.NodeType == ExpressionType.And || be.NodeType == ExpressionType.AndAlso)
            {
                if (be.Left.NodeType == ExpressionType.Constant)
                    return GetVale(be.Left) ? be.Right : Expression.Constant(false);

                if (be.Right.NodeType == ExpressionType.Constant)
                    return GetVale(be.Right) ? be.Left : Expression.Constant(false);

                return be;
            }
            else if (be.NodeType == ExpressionType.Or || be.NodeType == ExpressionType.OrElse)
            {
                if (be.Left.NodeType == ExpressionType.Constant)
                    return GetVale(be.Left) ? Expression.Constant(true) : be.Right;

                if (be.Right.NodeType == ExpressionType.Constant)
                    return GetVale(be.Right) ? Expression.Constant(true) : be.Left;

                return be;
            }

            if (be.Left.Type != typeof(bool))
                return be;

            if (be.NodeType == ExpressionType.Equal)
            {
                if (be.Left.NodeType == ExpressionType.Constant)
                    return GetVale(be.Left) ? be.Right : Visit(Expression.Not(be.Right));

                if (be.Right.NodeType == ExpressionType.Constant)
                    return GetVale(be.Right) ? be.Left : Visit(Expression.Not(be.Left));

                return be;
            }
            else if (be.NodeType == ExpressionType.NotEqual)
            {
                if (be.Left.NodeType == ExpressionType.Constant)
                    return GetVale(be.Left) ? Visit(Expression.Not(be.Right)) : be.Right;

                if (be.Right.NodeType == ExpressionType.Constant)
                    return GetVale(be.Right) ? Visit(Expression.Not(be.Left)) : be.Left;

                return be;
            }

            return be;
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            ConditionalExpression ce = (ConditionalExpression)base.VisitConditional(c);

            if (ce.Test.NodeType != ExpressionType.Constant)
                return ce;

            if (GetVale(ce.Test))
                return ce.IfTrue;
            else
                return ce.IfFalse;
        }
    }
}
