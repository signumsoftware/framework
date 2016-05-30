using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Utilities.ExpressionTrees
{
    class ReplaceTypeVisitor : ExpressionVisitor
    {
        public static Expression ReplaceExpression(Expression exp, Type fromType, Type toType)
        {
            return new ReplaceTypeVisitor { fromType = fromType, toType = toType }.Visit(exp);
        }

        Type fromType;
        Type toType;

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var obj = this.Visit(node.Object);
            var arguments = node.Arguments.Select(a => this.Visit(a)).ToArray();

            if (node.Method.IsGenericMethod)
            {
                var args = node.Method.GetGenericArguments().Select(a => this.ReplaceType(a)).ToArray();

                if (!args.SequenceEqual(node.Method.GetGenericArguments()))
                    return Expression.Call(obj, node.Method.GetGenericMethodDefinition().MakeGenericMethod(args), arguments);
            }

            return node.Update(obj, arguments);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.Quote)
            {
                var newType = this.ReplaceType(node.Type);
                var operand = this.Visit(node.Operand);
                if (newType != node.Type || operand != node.Operand)
                    return Expression.MakeUnary(node.NodeType, operand, newType);
                return node;
            }

            return base.VisitUnary(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var newType = this.ReplaceType(node.Type);
            var body = this.Visit(node.Body);
            var parameters = node.Parameters.Select(p => (ParameterExpression)this.Visit(p)).ToArray();
            if (newType != node.Type || body != node.Body || !parameters.SequenceEqual(node.Parameters))
                return Expression.Lambda(newType, body, parameters);
            return node;
        }

        Dictionary<ParameterExpression, ParameterExpression> parameterCache = new Dictionary<ParameterExpression, ParameterExpression>();

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node.Type != this.fromType)
                return node;

            return parameterCache.GetOrCreate(node, () => Expression.Parameter(this.toType, node.Name));
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            return base.VisitMember(node);
        }

        Type ReplaceType(Type type)
        {
            if (type.IsGenericType)
            {
                var arguments = type.GetGenericArguments().Select(a => ReplaceType(a)).ToArray();

                if (arguments.SequenceEqual(type.GetGenericArguments()))
                    return type;

                return type.GetGenericTypeDefinition().MakeGenericType(arguments);
            }

            if (type == this.fromType)
                return this.toType;

            return type;
        }
    }

    //Example
    //public class DescendatsMethodExpander : ReplaceTypeMethodExpander
    //{
    //    static Expression<Func<TreeEntity, IQueryable<TreeEntity>>> Expression =
    //        cp => Database.Query<TreeEntity>().Where(cc => (bool)cc.Route.IsDescendantOf(cp.Route));
    //    public DescendatsMethodExpander() : base(Expression) { }
    //}
    //[MethodExpander(typeof(DescendatsMethodExpander))]
    //public static IQueryable<T> Descendants<T>(this T e)
    //    where T : TreeEntity
    //{
    //    return Database.Query<T>().Where(cc => (bool)cc.Route.IsDescendantOf(e.Route));
    //}

    public class ReplaceTypeMethodExpander : IMethodExpander
    {
        LambdaExpression Expresion;

        public ReplaceTypeMethodExpander(LambdaExpression expression)
        {
            this.Expresion = expression;
        }

        public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
        {
            var genericType = mi.GetGenericMethodDefinition().GetGenericArguments().Single();
            var genericConstraint = genericType.GetGenericParameterConstraints().Single();
            var targetType = mi.GetGenericArguments().Single();
            var replacedLambdaExpression = (LambdaExpression)ReplaceTypeVisitor.ReplaceExpression(this.Expresion, genericConstraint, targetType);

            var invokeExpression = Expression.Invoke(replacedLambdaExpression, arguments);
            return ExpressionReplacer.Replace(invokeExpression);
        }
    }
}
