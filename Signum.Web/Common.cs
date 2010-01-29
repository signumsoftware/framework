using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Entities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.DataStructures;
using Signum.Engine;

namespace Signum.Web
{
    public delegate void CommonTask(BaseLine eb, TypeContext context);

    public static class Common
    {
        public static event CommonTask CommonTask;

        static Common()
        {
            CommonTask += new CommonTask(TaskSetLabelText);
            CommonTask += new CommonTask(TaskSetImplementations);
            CommonTask += new CommonTask(TaskSetReadOnly);
            CommonTask += new CommonTask(TaskSetValueLineType);
            CommonTask += new CommonTask(TaskSetHtmlProperties);
            CommonTask += new CommonTask(TaskSetReloadOnChange);
        }

        public static void FireCommonTasks(BaseLine eb, TypeContext context)
        {
            CommonTask(eb, context);
        }

        #region Tasks
        public static void TaskSetLabelText(BaseLine bl, TypeContext context)
        {
            if (bl != null && context.PropertyRoute.PropertyRouteType == PropertyRouteType.Property)
                bl.LabelText = context.PropertyRoute.PropertyInfo.NiceName();
        }

        public static void TaskSetImplementations(BaseLine bl, TypeContext context)
        {
            EntityBase eb = bl as EntityBase;
            if (eb != null)
            {
                PropertyRoute route = context.PropertyRoute;

                if (Reflector.IsMList(context.Type)) //if (eb is EntityList)
                    route = route.Add("Item");

                eb.Implementations = Schema.Current.FindImplementations(route);
            }
        }

        public static void TaskSetReadOnly(BaseLine bl, TypeContext context)
        {
            if (bl != null && context.PropertyRoute.PropertyRouteType == PropertyRouteType.Property)
            {
                if (context.PropertyRoute.PropertyInfo.IsReadOnly() || StyleContext.Current.ReadOnly)
                {
                    bl.ReadOnly = true;
                    bl.SetReadOnly();
                }
            }
        }

        public static void TaskSetValueLineType(BaseLine bl, TypeContext context)
        {
            ValueLine vl = bl as ValueLine;
            if (vl != null && context.PropertyRoute.PropertyRouteType == PropertyRouteType.Property)
            {
                if (context.PropertyRoute.PropertyInfo.HasAttribute<DateOnlyValidatorAttribute>())
                    vl.ValueLineType = ValueLineType.Date;
            }
        }

        public static void TaskSetHtmlProperties(BaseLine bl, TypeContext context)
        {
            ValueLine vl = bl as ValueLine;
            if (vl != null && context.PropertyRoute.PropertyRouteType == PropertyRouteType.Property)
            {
                var atribute = context.PropertyRoute.PropertyInfo.SingleAttribute<StringLengthValidatorAttribute>();
                if (atribute != null)
                {
                    int max = atribute.Max; //-1 if not set
                    if (max != -1)
                    {
                        vl.ValueHtmlProps.AddRangeFromAnonymousType(new
                        {
                            maxlength = max,
                            size = max
                        });
                    }
                }
            }
        }

        public static void TaskSetReloadOnChange(BaseLine bl, TypeContext context)
        {
            if (bl != null)
            {
                var atribute = context.PropertyRoute.PropertyInfo.SingleAttribute<ReloadEntityOnChange>();
                if (atribute != null)
                    bl.ReloadOnChange = true;
            }
        }
#endregion

        static MethodInfo mi = ReflectionTools.GetMethodInfo(() => Common.WalkExpression<TypeDN, TypeDN>(null, null)).GetGenericMethodDefinition();
        //static MethodInfo mi = typeof(Common).GetMethod("WalkExpression", BindingFlags.Static | BindingFlags.Public); 

        internal static TypeContext UntypedTypeContext(TypeContext tc, LambdaExpression lambda, Type returnType)
        {
            return (TypeContext)mi.GenericInvoke(new[] { tc.Type, returnType }, null, new object[] { tc, lambda });
        }

        public static TypeContext<S> WalkExpression<T, S>(TypeContext<T> tc, Expression<Func<T, S>> lambda)
        {
            return MemberAccessGatherer.WalkExpression(tc, lambda);

        }
    }

    enum TypeContextNodeType
    { 
        TypeContext,
        AbstractContext,
        TypeSubContext
    }

    class TypeContextExpression : Expression
    {
        public readonly PropertyInfo[] Properties;
        
        internal TypeContextExpression(PropertyInfo[] properties, Type type)
            :base((ExpressionType)TypeContextNodeType.TypeContext, type)
        {
            this.Properties = properties;

            if (!Schema.Current.Tables.ContainsKey(type))
                throw new InvalidOperationException(Web.Properties.Resources.Type0HasToBeInTheSchema.Formato(type)); 
        }

        protected TypeContextExpression(PropertyInfo[] properties, Type type, TypeContextNodeType nodeType)
            :base((ExpressionType)nodeType, type)
        {
            this.Properties = properties;
        }

        public override string ToString()
        {
            return "TypeContext<{0}>".Formato(Type.Name);
        }

        public static TypeContextExpression RootOrAbstract(PropertyInfo[] properties, Type type)
        {
            if(Schema.Current.Tables.ContainsKey(type))
                return new TypeContextExpression(properties, type); 
            else 
                return new AbstractContextExpression(properties, type); 
        }

        public virtual PropertyRoute Route
        {
            get { return PropertyRoute.Root(Type); }
        }
    }

    class AbstractContextExpression : TypeContextExpression
    {
        internal AbstractContextExpression(PropertyInfo[] properties, Type type)
            :base(properties, type, TypeContextNodeType.AbstractContext)
        {
            if(!typeof(IIdentifiable).IsAssignableFrom(type))
                throw new InvalidOperationException(Web.Properties.Resources.Type0HasToBeIIdentifiableAtLeast.Formato(type.Name));
        }

        public override PropertyRoute Route
        {
            get { throw new InvalidOperationException(Web.Properties.Resources.AbstractContextHasNotRoute); }
        }
    }

    class TypeSubContextExpression : TypeContextExpression
    {
        private PropertyRoute route;
        public override PropertyRoute Route
        {
            get { return route; }
        }

         public TypeSubContextExpression(PropertyInfo[] properties, PropertyRoute route)
            :base(properties, route.Type, TypeContextNodeType.TypeSubContext)
        {
            this.route = route;
        }
    }
    
    internal class MemberAccessGatherer : ExpressionVisitor
    {
        public Dictionary<ParameterExpression, TypeContextExpression> replacements = new Dictionary<ParameterExpression,TypeContextExpression>();

        public static TypeContext<S> WalkExpression<T, S>(TypeContext<T> tc, Expression<Func<T, S>> lambda)
        {
            var mag = new MemberAccessGatherer()
            {
                replacements = { { lambda.Parameters[0], (tc is TypeSubContext<T>) ? 
                                     new TypeSubContextExpression(new PropertyInfo[0], tc.PropertyRoute) :
                                     new TypeContextExpression(new PropertyInfo[0], typeof(T)) } }
            };

            TypeContextExpression result = Cast(mag.Visit(lambda.Body));

            S value = lambda.Compile()(tc.Value);

            if (result is TypeSubContextExpression)
                return new TypeSubContext<S>(value, tc, result.Properties, ((TypeSubContextExpression)result).Route); 
            if (result is AbstractContextExpression)
                throw new InvalidOperationException(Properties.Resources.ExpressionCannotFinishWithAnTypeWithImplementations0.Formato(lambda.NiceToString()));
            return new TypeContext<S>(value, TypeContext.Compose(tc.Name, result.Properties.Select(a => a.Name)));            
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return replacements.GetOrThrow(p, Properties.Resources.TypeSubContextCanNotBeCreated0.Formato(p.NiceToString()));
        }

        static TypeContextExpression Cast(Expression expression)
        {
            var result = expression as TypeContextExpression;
            if (result == null)
                throw new InvalidOperationException(Properties.Resources.TypeSubContextCanNotBeCreated0.Formato(expression == null? null : expression.NiceToString()));
            return result;
        }

        protected override Expression VisitMemberAccess(MemberExpression me)
        {
            var tce = Cast(Visit(me.Expression));

            if (!typeof(Lite).IsAssignableFrom(tce.Type) && (me.Member.Name == "EntityOrNull" && me.Member.Name == "Entity"))
                return TypeContextExpression.RootOrAbstract(tce.Properties, me.Type);
            
            return new TypeSubContextExpression(tce.Properties.And((PropertyInfo)me.Member).ToArray(), tce.Route.Add((PropertyInfo)me.Member));
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.TypeAs || u.NodeType == ExpressionType.Convert)
            {
                var tce = Cast(Visit(u.Operand));
                return TypeContextExpression.RootOrAbstract(tce.Properties, u.Type);
            }

            return base.VisitUnary(u);
        }

        static string[] tryies =new string[]{"TryCC", "TryCS", "TrySS", "TrySC"};

        MethodInfo miRetrieve = ReflectionTools.GetMethodInfo((Lite<TypeDN> l) => l.Retrieve()).GetGenericMethodDefinition();

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.IsInstantiationOf(miRetrieve))
            {
                var tce = Cast(Visit(m.Arguments[0]));
                return TypeContextExpression.RootOrAbstract(tce.Properties, m.Type);
            }

            if(m.Method.DeclaringType == typeof(Extensions) && tryies.Contains(m.Method.Name))
            {
                var tce = Cast(Visit(m.Arguments[0]));
                var lambda = (LambdaExpression)m.Arguments[1];

                replacements.Add(lambda.Parameters[0], tce);

                return Cast(Visit(lambda.Body));
            }

            return base.VisitMethodCall(m); 
        }
    }
}
