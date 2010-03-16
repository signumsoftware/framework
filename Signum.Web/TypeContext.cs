using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using Signum.Entities.Reflection;
using Signum.Web.Properties;
using Signum.Utilities.Reflection;
using Signum.Engine;

namespace Signum.Web
{
    #region TypeContextHelper
    public static class TypeContextHelper
    {
        public static TypeContext<T> TypeContext<T>(this HtmlHelper helper)
        {
            TypeContext<T> tc = CastTypeContext<T>(helper.ViewData.Model as TypeContext);
            if (tc != null)
                return tc;

            if (helper.ViewData.ContainsKey(ViewDataKeys.TypeContextKey))
            {
                tc = CastTypeContext<T>(helper.ViewData[helper.ViewData[ViewDataKeys.TypeContextKey].ToString()] as TypeContext);
                if (tc != null)
                {
                    WriteRuntimeAndId<T>(helper, tc);
                    return tc;
                }
                return helper.BeginContext<T>((T)helper.ViewData[helper.ViewData[ViewDataKeys.TypeContextKey].ToString()], helper.ViewData[ViewDataKeys.TypeContextKey].ToString(), true);
            }

            return helper.BeginContext<T>((T)helper.ViewData.Model, null, null);
        }

        public static TypeContext<T> TypeContext<T>(this HtmlHelper helper, bool writeIdAndRuntime)
        {
            TypeContext<T> tc = CastTypeContext<T>(helper.ViewData.Model as TypeContext);
            if (tc != null)
                return tc;

            if (helper.ViewData.ContainsKey(ViewDataKeys.TypeContextKey))
            {
                tc = CastTypeContext<T>(helper.ViewData[helper.ViewData[ViewDataKeys.TypeContextKey].ToString()] as TypeContext);
                if (tc != null)
                {
                    if (writeIdAndRuntime)
                        WriteRuntimeAndId<T>(helper, tc);
                    return tc;
                }
                return helper.BeginContext<T>((T)helper.ViewData[helper.ViewData[ViewDataKeys.TypeContextKey].ToString()], helper.ViewData[ViewDataKeys.TypeContextKey].ToString(), writeIdAndRuntime);
            }

            return helper.BeginContext<T>((T)helper.ViewData.Model, null, writeIdAndRuntime);
        }

        public static TypeContext<T> TypeContext<T>(this HtmlHelper helper, string viewDataKeyAndPrefix)
        {
            if (!viewDataKeyAndPrefix.HasText())
                return TypeContext<T>(helper);

            TypeContext<T> tc = CastTypeContext<T>(helper.ViewData[viewDataKeyAndPrefix] as TypeContext);
            if (tc != null)
            {
                WriteRuntimeAndId<T>(helper, tc);
                return tc;
            }

            return helper.BeginContext<T>((T)helper.ViewData[viewDataKeyAndPrefix], viewDataKeyAndPrefix, true);
        }

        public static TypeContext<T> TypeContext<T>(this HtmlHelper helper, string viewDataKeyAndPrefix, bool writeIdAndRuntime)
        {
            if (!viewDataKeyAndPrefix.HasText())
                return TypeContext<T>(helper, writeIdAndRuntime);

            TypeContext<T> tc = CastTypeContext<T>(helper.ViewData[viewDataKeyAndPrefix] as TypeContext);
            if (tc != null)
            {
                if (writeIdAndRuntime)
                    WriteRuntimeAndId<T>(helper, tc);
                return tc;
            }

            return helper.BeginContext<T>((T)helper.ViewData[viewDataKeyAndPrefix], viewDataKeyAndPrefix, writeIdAndRuntime);
        }

        static TypeContext<T> CastTypeContext<T>(TypeContext typeContext)
        {
            if (typeContext == null)
                return null;

            if (typeContext is TypeContext<T>)
                return (TypeContext<T>)typeContext;

            if (typeContext.Type.IsAssignableFrom(typeof(T)))
            {
                ParameterExpression pe = Expression.Parameter(typeContext.Type, "p");
                LambdaExpression lambda = Expression.Lambda(Expression.Convert(pe, typeof(T)), pe);
                return (TypeContext<T>)Common.UntypedTypeContext(typeContext, lambda, typeof(T));
            }

            return null;
        }

        static MethodInfo mi = ReflectionTools.GetMethodInfo((Lite<TypeDN> l) => l.Retrieve()).GetGenericMethodDefinition();

        public static TypeContext ExtractLite<T>(this TypeContext<T> liteTypeContext)
        {
            if (!typeof(Lite).IsAssignableFrom(liteTypeContext.Type))
                return null;

            ParameterExpression pe = Expression.Parameter(liteTypeContext.Type, "p");
            Expression call = Expression.Call(pe, mi.MakeGenericMethod(Reflector.ExtractLite(liteTypeContext.Type)), pe);
            LambdaExpression lambda = Expression.Lambda(call, pe);
            return Common.UntypedTypeContext(liteTypeContext, lambda, Reflector.ExtractLite(liteTypeContext.Type));
        }

        static TypeContext<T> BeginContext<T>(this HtmlHelper helper, T value, string prefix, bool? writeIdAndRuntime)
        {
            TypeContext<T> tc = new TypeContext<T>(value, prefix);

            if (helper.ViewData.ContainsKey(ViewDataKeys.Reactive))
                helper.Write("<input type='hidden' id='{0}' name='{0}' value='' />\n".Formato(ViewDataKeys.Reactive));

            if (!writeIdAndRuntime.HasValue || writeIdAndRuntime.Value)
                WriteRuntimeAndId<T>(helper, tc);

            if (typeof(ImmutableEntity).IsAssignableFrom(typeof(T)) && !((IIdentifiable)value).IsNew) 
            {
                StyleContext sc = new StyleContext() { ReadOnly = true };
                tc.OwnsStyleContext = true;
            }
            if (helper.ViewData.ContainsKey(ViewDataKeys.StyleContext))
            {
                ((StyleContext)helper.ViewData[ViewDataKeys.StyleContext]).Register();
                tc.OwnsStyleContext = true;
            }

            return tc;
        }

        private static void WriteRuntimeAndId<T>(this HtmlHelper helper, TypeContext<T> tc)
        {
            if (helper.WriteIdAndRuntime(tc))
            {
                //RuntimeInfo runtimeInfo = null;
                //if (typeof(IdentifiableEntity).IsAssignableFrom(typeof(T)))
                //    runtimeInfo = new RuntimeInfo<T>(tc.Value);
                //else if (typeof(EmbeddedEntity).IsAssignableFrom(typeof(T)))
                //    runtimeInfo = new EmbeddedRuntimeInfo<T>(tc.Value, false);
                //else if (Reflector.IsMList(typeof(T)))
                //    runtimeInfo = new RuntimeInfo { RuntimeType = typeof(T), IsNew = false };

                    helper.WriteEntityInfo(helper.GlobalName(tc.Name), new RuntimeInfo(tc.Value), new StaticInfo(typeof(T)));
            }

            //Avoid subcontexts to write their id and runtime, only the main embedded typecontext must write them
            if (helper.ViewData.ContainsKey(ViewDataKeys.WriteSFInfo))
                helper.ViewData.Remove(ViewDataKeys.WriteSFInfo);
        }

        public static TypeContext<S> TypeContext<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            return TypeContext<T, S>(helper, parent, property, true);
        }

        public static TypeContext<S> TypeContext<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property, bool writeSFInfo)
        {
            TypeSubContext<S> typeContext = (TypeSubContext<S>)Common.WalkExpression(parent, property);
            if (writeSFInfo)
            {
                helper.ViewData[ViewDataKeys.WriteSFInfo] = true;
                WriteRuntimeAndId(helper, typeContext);
            }
            return typeContext;
        }
        
        public static IEnumerable<TypeElementContext<S>> TypeElementContext<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, IList<S>>> property)
        {
            using (TypeContext<IList<S>> context = (TypeContext<IList<S>>)Common.WalkExpression(parent, property))
            {
                for (int i = 0; i < context.Value.Count; i++)
                {
                    var econtext = new TypeElementContext<S>(context.Value[i], context, i);
                    yield return econtext;
                }
            }
        }
    }
    #endregion

    #region TypeContext
    public abstract class TypeContext : IDisposable
    {
        public const string Separator = "_";
        public const string Id = "sfId";
        public const string StaticType = "sfStaticType"; //READONLY
        public const string RuntimeType = "sfRuntimeType";
        public const string Ticks = "sfTicks";
        public const string CssLineLabel = "labelLine";
        
        public bool ownsStyleContext = false;
        public bool OwnsStyleContext 
        { 
            get { return ownsStyleContext; } 
            set { ownsStyleContext = value; } 
        }

        public static string Compose(string prefix, params string[] namesToAppend)
        {
            return Compose(prefix, (IEnumerable<string>)namesToAppend);
        }

        public static string Compose(string prefix, IEnumerable<string> namesToAppend)
        {
            string result = prefix;
            foreach (string s in namesToAppend)
                result += Separator + s;
            return result;
        }

        public abstract object UntypedValue { get; }
        public abstract string Name { get; }

        public abstract Type Type { get; }
        public abstract PropertyRoute PropertyRoute { get; }

        public virtual void Dispose()
        {
            if (ownsStyleContext)
                StyleContext.Current.Dispose();
        }
    }
    #endregion

    #region TypeContext<T>
    public class TypeContext<T> : TypeContext
    {
        public T Value { get; set; }
        string prefix;

        public override object UntypedValue
        {
            get { return Value; }
        }

        public TypeContext(T value)
        {
            Value = value;
        }

        public TypeContext(T value, string prefix)
        {
            Value = value;
            this.prefix = prefix; 
        }

        public override string Name
        {
            get 
            {
                return prefix.HasText() ? prefix : ""; //TypeContext.Separator;
            }
        }

        public override Type Type
        {
            get { return typeof(T); }
        }

        public override PropertyRoute PropertyRoute
        {
            get { return PropertyRoute.Root(typeof(T)); }
        }

        internal virtual TypeContextExpression CreateExpression()
        {
            return new TypeContextExpression(new PropertyInfo[0], typeof(T));
        }
    }
    #endregion

    #region TypeSubContext<T>
    public class TypeSubContext<T> : TypeContext<T>, IDisposable
    {
        PropertyInfo[] properties; 
        public TypeContext Parent { get; private set; }
        PropertyRoute route;

        public TypeSubContext(T value, TypeContext parent, PropertyInfo[] properties, PropertyRoute route)
            : base(value)
        {
            this.properties = properties;
            this.Parent = parent;
            this.route = route;
        }

        public PropertyInfo[] Properties
        {
            get { return properties; }
        }


        public override string Name
        {
            get 
            {
                return TypeContext.Compose(Parent.Name, properties.Select(a => a.Name));
            }
        }

        public override PropertyRoute PropertyRoute
        {
            get
            {
                return route;
            }
        }

        internal override TypeContextExpression CreateExpression()
        {
            return new TypeSubContextExpression(new PropertyInfo[0], PropertyRoute);
        }
    }
    #endregion

    #region TypeElementContext<T>
    public class TypeElementContext<T> : TypeContext<T>, IDisposable
    {
        int Index;

        internal TypeContext Parent { get; private set; }
        PropertyRoute route; 

        public TypeElementContext(T value, TypeContext parent, int index)
            : base(value)
        {
            this.Index = index;
            Parent = parent;
            route = parent.PropertyRoute.Add("Item"); 
        }

        public override string Name
        {
            get
            {
                return TypeContext.Compose(Parent.Name, Index.ToString());
            }
        }

        public override PropertyRoute PropertyRoute
        {
            get { return route; }
        }

        internal override TypeContextExpression CreateExpression()
        {
            return new TypeElementContextExpression(new PropertyInfo[0], PropertyRoute, Index);
        }
    }
    #endregion
}
