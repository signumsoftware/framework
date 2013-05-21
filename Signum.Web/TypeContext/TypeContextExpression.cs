using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine;
using Signum.Entities.Reflection;
using System.Collections.Concurrent;
using Signum.Entities.Basics;

namespace Signum.Web
{
    enum TypeContextNodeType
    {
        TypeContext = 1000,
    }


    class TypeContextExpression : Expression
    {
        readonly PropertyRoute route;
        public PropertyRoute Route
        {
            get { return route; }
        }

        readonly Type type; 
        public override Type Type
        {
            get { return type; }
        }

        public override ExpressionType NodeType
        {
            get{return (ExpressionType)TypeContextNodeType.TypeContext; }
        }

        public readonly PropertyInfo[] Properties;

        internal TypeContextExpression(PropertyInfo[] properties, Type type, PropertyRoute route)
        {
            this.type = type;
            this.Properties = properties;
            this.route = route;
        }

        public override string ToString()
        {
            return "TypeSubContext<{0}>".Formato(Type.Name);
        }
    }

    internal class MemberAccessGatherer : SimpleExpressionVisitor
    {
        static ConcurrentDictionary<LambdaExpression, Delegate> compiledExpressions = new ConcurrentDictionary<LambdaExpression, Delegate>(ExpressionComparer.GetComparer<LambdaExpression>());

        public Dictionary<ParameterExpression, TypeContextExpression> replacements = new Dictionary<ParameterExpression, TypeContextExpression>();

        public static TypeContext<S> WalkExpression<T, S>(TypeContext<T> tc, Expression<Func<T, S>> lambda)
        {
            var mag = new MemberAccessGatherer()
            {
                replacements = { { lambda.Parameters[0], new TypeContextExpression(new PropertyInfo[0], typeof(T), tc.PropertyRoute) } }
            };

            TypeContextExpression result = Cast(mag.Visit(lambda.Body));

            Func<T, S> func = (Func<T, S>)compiledExpressions.GetOrAdd(lambda, ld => ld.Compile());

            S value = func(tc.Value);

            return new TypeSubContext<S>(value, tc, result.Properties, result.Route);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return replacements.GetOrThrow(p, "TypeSubContext can not be created: {0}".Formato(p.NiceToString()));
        }

        static TypeContextExpression Cast(Expression expression)
        {
            var result = expression as TypeContextExpression;
            if (result == null)
                throw new InvalidOperationException("TypeSubContext can not be created: {0}".Formato(expression == null ? null : expression.NiceToString()));
            return result;
        }

        protected override Expression VisitMemberAccess(MemberExpression me)
        {
            var tce = Cast(Visit(me.Expression));
            
            if (tce.Type.IsLite() && (me.Member.Name == "EntityOrNull" || me.Member.Name == "Entity"))
                return new TypeContextExpression(tce.Properties, me.Type, tce.Route.Add((PropertyInfo)me.Member));

            return new TypeContextExpression(
                tce.Properties.And((PropertyInfo)me.Member).ToArray(),
                me.Type,
                tce.Route.Add((PropertyInfo)me.Member));
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.TypeAs || u.NodeType == ExpressionType.Convert)
            {
                var tce = Cast(Visit(u.Operand));
                return new TypeContextExpression(tce.Properties, u.Type, PropertyRoute.Root(u.Type));
            }

            return base.VisitUnary(u);
        }

        static string[] tryies = new string[] { "TryCC", "TryCS", "TrySS", "TrySC" };
        
        MethodInfo miRetrieve = ReflectionTools.GetMethodInfo((Lite<TypeDN> l) => l.Retrieve()).GetGenericMethodDefinition();

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.IsInstantiationOf(miRetrieve))
            {
                var tce = Cast(Visit(m.Arguments[0]));
                return new TypeContextExpression(tce.Properties, m.Type, tce.Route.Add("Entity"));
            }

            if (m.Method.DeclaringType == typeof(Extensions) && tryies.Contains(m.Method.Name))
            {
                var tce = Cast(Visit(m.Arguments[0]));
                var lambda = (LambdaExpression)m.Arguments[1];

                replacements.Add(lambda.Parameters[0], tce);

                return Cast(Visit(lambda.Body));
            }

            if (m.Method.IsInstantiationOf(MixinDeclarations.miMixin))
            {
                var tce = Cast(Visit(m.Object));
                var mixinType = m.Method.GetGenericArguments().SingleEx();
                return new TypeContextExpression(tce.Properties, mixinType, tce.Route.Add(mixinType));
            }

            return base.VisitMethodCall(m);
        }
    }
}
