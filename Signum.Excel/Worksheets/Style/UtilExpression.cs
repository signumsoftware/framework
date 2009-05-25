using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using Signum.Utilities.ExpressionTrees;
using System.Reflection;
using System.Collections.Specialized;
using Signum.Utilities;

namespace Signum.Excel
{
    static class UtilExpression
    {
        public static Expression ListInit<T>(IEnumerable<T> collection) where T : IExpressionWriter
        {
            return ListInit(collection, a => a.CreateExpression());
        }

        public static Expression ListInit<T>(IEnumerable<T> collection, Func<T, Expression> func)
        {
            if (collection.Count() == 0)
                return Expression.New(collection.GetType());
            else
                return Expression.ListInit(Expression.New(collection.GetType()), collection.Select(func));
        }

        public static Expression MemberInit<T>(TrioList<T> trios) where T : new()
        {
            if (trios.Count == 0)
                return Expression.New(typeof(T));
            else
                return Expression.MemberInit(Expression.New(typeof(T)),
                    trios.Select(t => (MemberBinding)Expression.Bind(t.Member, t.RightSide)));
        }

        public static Expression MemberInit<T>(Expression<Func<T>> constructor,  TrioList<T> trios)
        {
            if (trios.Count == 0)
                return (NewExpression)constructor.Body;
            else
            return Expression.MemberInit((NewExpression)constructor.Body,
                trios.Select(t => (MemberBinding)Expression.Bind(t.Member, t.RightSide)));
        }

        public static Expression Collapse(this Expression expression)
        {
            return Expression.Call(typeof(Tree).GetMethod("Collapse").MakeGenericMethod(expression.Type), expression);
        }
    }

    public class Trio<T>
    {
        public Expression RightSide;
        public MemberInfo Member;
    }

    public class TrioList<T> : Collection<Trio<T>>
    {
        public void Add<S>(S defaultValue, S valor, Expression<Func<T, S>> exp)
        {
            if (!object.Equals(defaultValue,valor))
                Add(Expression.Constant(valor), exp);
        }

        public void Add<S>(S expressionWriter, Expression<Func<T, S>> exp) where S:IExpressionWriter
        {
            if (expressionWriter != null)
                Add( expressionWriter.CreateExpression(), exp);
        }

        public void Add(Collection<string> stringCollection, Expression<Func<T, Collection<string>>> exp)
        {
            if (stringCollection != null)
                Add(UtilExpression.ListInit(stringCollection, s => Expression.Constant(s)), exp);
        }

        void Add<S>(Expression rightSide, Expression<Func<T, S>> exp)
        {
            Expression exp2 = exp.Body;
            if(exp.Body.NodeType == ExpressionType.Convert || exp.Body.NodeType == ExpressionType.ConvertChecked)
                exp2 = ((UnaryExpression)exp2).Operand;

            if(exp2.NodeType != ExpressionType.MemberAccess)
                  throw new ApplicationException("Invalid lambda {0}".Formato(exp.GenerateCSharpCode()));

            this.Add(new Trio<T>
            {
                RightSide = rightSide,
                Member = ((MemberExpression)exp2).Member
            }); 
        }

    }
}
