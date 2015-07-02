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

    class TypeContextExpression : Expression
    {
        readonly object value;
        public object Value
        {
            get { return value; }
        }

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
            get { return ExpressionType.Extension; }
        }

        public readonly PropertyInfo[] Properties;

        internal TypeContextExpression(PropertyInfo[] properties, Type type, PropertyRoute route, object value)
        {
            this.type = type;
            this.Properties = properties;
            this.route = route;
            this.value = value; 
        }

        public PropertyRoute AddDynamic(MemberInfo mi)
        {
            if (Value is Entity)
                return PropertyRoute.Root(value.GetType()).Add(mi);
            else
                return Route.Add(mi); 
        }

        public override string ToString()
        {
            return "TypeSubContext<{0}>".FormatWith(Type.Name);
        }
    }

    internal class MemberAccessGatherer : ExpressionVisitor
    {
        static object NonValue = new object(); 

        public Dictionary<ParameterExpression, TypeContextExpression> replacements = new Dictionary<ParameterExpression, TypeContextExpression>();

        public static TypeContext<S> WalkExpression<T, S>(TypeContext<T> tc, Expression<Func<T, S>> lambda)
        {
            var mag = new MemberAccessGatherer()
            {
                replacements = { { lambda.Parameters[0], new TypeContextExpression(new PropertyInfo[0], typeof(T), tc.PropertyRoute, tc.Value) } }
            };

            TypeContextExpression result = Cast(mag.Visit(lambda.Body));

            var value = result.Value == NonValue ?
                (S)(object)null :
                (S)result.Value;

            return new TypeContext<S>(value, tc, result.Properties.ToString(a => a.Name, TypeContext.Separator), result.Route);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return replacements.GetOrThrow(p, "TypeSubContext can not be created: {0}".FormatWith(p.ToString()));
        }

        static TypeContextExpression Cast(Expression expression)
        {
            var result = expression as TypeContextExpression;
            if (result == null)
                throw new InvalidOperationException("TypeSubContext can not be created: {0}".FormatWith(expression == null ? null : expression.ToString()));
            return result;
        }

        protected override Expression VisitMember(MemberExpression me)
        {
            var tce = Cast(Visit(me.Expression));

            if (tce.Value == null)
                throw new InvalidOperationException("Impossible to access member {0} of null reference".FormatWith(me.Member.Name)); 

            if (tce.Type.IsLite() && (me.Member.Name == "EntityOrNull" || me.Member.Name == "Entity"))
            {
                var lite = tce.Value as Lite<IEntity>;

                return new TypeContextExpression(tce.Properties, me.Type,
                    tce.AddDynamic(me.Member),
                    tce.Value == NonValue ? NonValue :
                    me.Member.Name == "EntityOrNull" ? lite.EntityOrNull : lite.Entity);
            }
            else
            {
                var field = tce.Value == NonValue ? NonValue :
                    me.Member is PropertyInfo ?
                    ((PropertyInfo)me.Member).GetValue(tce.Value, null) :
                    ((FieldInfo)me.Member).GetValue(tce.Value);

                return new TypeContextExpression(
                    tce.Properties.And((PropertyInfo)me.Member).ToArray(),
                    me.Type,
                    tce.AddDynamic(me.Member),
                    field);
            }
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.TypeAs || u.NodeType == ExpressionType.Convert)
            {
                var tce = Cast(Visit(u.Operand));
                return new TypeContextExpression(tce.Properties, u.Type,
                    PropertyRoute.Root(tce.Value == NonValue ? u.Type : tce.Value.GetType()),
                    tce.Value == NonValue ? NonValue :
                    tce.Value);
            }

            return base.VisitUnary(u);
        }

        static readonly PropertyInfo piEntity = ReflectionTools.GetPropertyInfo((Lite<IEntity> lite) => lite.Entity);
        
        static readonly MethodInfo miRetrieve = ReflectionTools.GetMethodInfo((Lite<TypeEntity> l) => l.Retrieve()).GetGenericMethodDefinition();

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.IsInstantiationOf(miRetrieve))
            {
                var tce = Cast(Visit(m.Arguments[0]));

                Lite<IEntity> lite = tce.Value as Lite<IEntity>;

                var obj =  tce.Value == NonValue ? NonValue:
                    lite.Retrieve();

                return new TypeContextExpression(tce.Properties, m.Type, 
                    tce.Route.Add(piEntity), obj);
            }

            if (m.Method.IsInstantiationOf(MixinDeclarations.miMixin))
            {
                var tce = Cast(Visit(m.Object));
                var mixinType = m.Method.GetGenericArguments().SingleEx();

                var ident = tce.Value as Entity;

                return new TypeContextExpression(tce.Properties, mixinType, tce.Route.Add(mixinType),
                    tce.Value == NonValue ? NonValue :
                    ident.GetMixin(mixinType));
            }

            return base.VisitMethodCall(m);
        }

        internal static string GetName(MemberInfo m)
        {
            if (m is PropertyInfo || m is FieldInfo)
            {
                if (m.DeclaringType.IsLite() && (m.Name == "EntityOrNull" || m.Name == "Entity"))
                    return null;

                return m.Name;
            }

            return null;
        }
    }
}
