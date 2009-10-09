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

            if (typeContext.ContextType.IsAssignableFrom(typeof(T)))
            {
                ParameterExpression pe = Expression.Parameter(typeContext.ContextType, "p");
                LambdaExpression lambda = Expression.Lambda(Expression.Convert(pe, typeof(T)), pe);
                return (TypeContext<T>)Common.UntypedTypeContext(typeContext, lambda, typeof(T));
            }

            return null;
        }

        static TypeContext<T> BeginContext<T>(this HtmlHelper helper, T value, string prefix, bool? writeIdAndRuntime)
        {
            TypeContext<T> tc = new TypeContext<T>(value, prefix);

            if (helper.ViewData.ContainsKey(ViewDataKeys.Reactive))
                helper.Write("<input type='hidden' id='{0}' name='{0}' value='' />\n".Formato(ViewDataKeys.Reactive));

            if (!writeIdAndRuntime.HasValue || writeIdAndRuntime.Value)
                WriteRuntimeAndId<T>(helper, tc);

            if (typeof(ImmutableEntity).IsAssignableFrom(typeof(T)) && value != null && 
                typeof(IIdentifiable).IsAssignableFrom(typeof(T)) && !((IIdentifiable)value).IsNew)
            {
                StyleContext sc = new StyleContext() { ReadOnly = true };
            }
            return tc;
        }

        private static void WriteRuntimeAndId<T>(this HtmlHelper helper, TypeContext<T> tc)
        {
            if (helper.WriteIdAndRuntime())
            {
                if (typeof(IdentifiableEntity).IsAssignableFrom(typeof(T)))
                {
                    IdentifiableEntity id = (IdentifiableEntity)(object)tc.Value;

                    if (tc.Value != null)
                        helper.ViewContext.HttpContext.Response.Write(
                            helper.Hidden(helper.GlobalPrefixedName(Signum.Web.TypeContext.Separator + Signum.Web.TypeContext.RuntimeType), typeof(T).Name) + "\n");
                    else
                    {
                        //helper.ViewContext.HttpContext.Response.Write(
                        //    helper.Hidden(helper.GlobalPrefixedName(Signum.Web.TypeContext.Separator + Signum.Web.TypeContext.RuntimeType), "") + "\n");
                        helper.ViewContext.HttpContext.Response.Write(
                            helper.Hidden(helper.GlobalPrefixedName(Signum.Web.TypeContext.Separator + Signum.Web.TypeContext.StaticType), typeof(T).Name) + "\n");
                    }

                    helper.ViewContext.HttpContext.Response.Write(
                        helper.Hidden(helper.GlobalPrefixedName(Signum.Web.TypeContext.Separator + Signum.Web.TypeContext.Id), id.TryCS(i => i.IdOrNull)) + "\n");
                }
                else if (typeof(EmbeddedEntity).IsAssignableFrom(typeof(T)))
                {
                    helper.ViewContext.HttpContext.Response.Write(
                        helper.Hidden(helper.GlobalPrefixedName(Signum.Web.TypeContext.Separator + Signum.Web.TypeContext.RuntimeType), typeof(T).Name) + "\n");
                }
            }
            //Avoid subcontexts to write their id and runtime, only the main embedded typecontext must write them
            if (helper.ViewData.ContainsKey(ViewDataKeys.EmbeddedControl))
                helper.ViewData.Remove(ViewDataKeys.EmbeddedControl);
        }

        public static TypeContext<S> TypeContext<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            TypeSubContext<S> typeContext = (TypeSubContext<S>)Common.WalkExpression(parent, property);
            return typeContext;
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

        public static string Compose(string prefix, params string[] namesToAppend)
        {
            string result = prefix;
            foreach (string s in namesToAppend)
                result += Separator + s;
            return result;
        }

        public abstract object UntypedValue { get; }
        public abstract string Name { get; }

        public abstract string FriendlyName { get; }
        public abstract Type ContextType { get; }
        public abstract Type LastIdentifiableProperty { get; }
        public abstract List<PropertyInfo> GetPath();
        public abstract PropertyInfo LastProperty { get; }

        public virtual void Dispose()
        {
            //Do nothing
        }
    }
    #endregion

    #region TypeContext<T>
    public class TypeContext<T> : TypeContext
    {
        public T Value { get; set; }
        string prefix;

        public override Type LastIdentifiableProperty
        {
            get 
            {
                if (!Reflector.IsIdentifiableEntity(typeof(T)))
                    throw new ApplicationException(Resources.NoIdentifiableEntityPropertyInTypeContextPath);
                return typeof(T); 
            }
        }

        public override List<PropertyInfo> GetPath() 
        {
            return new List<PropertyInfo>();
        }

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

        public override string FriendlyName
        {
            get { throw new NotImplementedException("TypeContext has no DisplayName"); }
        }

        public override PropertyInfo LastProperty
        {
	         get { throw new NotImplementedException("TypeContext has no Property"); }
        }

        public override Type ContextType
        {
            get { return typeof(T); }
        }
        
        public override void Dispose()
        {
            if (typeof(ImmutableEntity).IsAssignableFrom(typeof(T)))
                StyleContext.Current.Dispose();
        }
    }
    #endregion

    #region TypeSubContext<T>
    public class TypeSubContext<T> : TypeContext<T>, IDisposable
    {
        PropertyInfo[] properties; 
        internal TypeContext Parent { get; private set; }

        public TypeSubContext(T value, TypeContext parent, PropertyInfo[] properties)
            : base(value)
        {
            this.properties = properties;
            Parent = parent;
        }

        public PropertyInfo[] Properties
        {
            get { return properties; }
        }

        public override PropertyInfo LastProperty
        {
            get { return properties.Last(); }
        }

        public override Type LastIdentifiableProperty
        {
            get
            {
                return properties.LastOrDefault(pi => Reflector.IsIdentifiableEntity(pi.PropertyType))
                                 .TryCC(prop => prop.PropertyType) ??
                                 Parent.LastIdentifiableProperty;
            }
        }

        public override List<PropertyInfo> GetPath()
        {
            List<PropertyInfo> pis = Parent.GetPath();
            pis.AddRange(properties);
            
            Type lastIP = LastIdentifiableProperty;
            int lastPI = pis.FindLastIndex(pi => pi.PropertyType == lastIP); 

            if (lastPI == -1)
                return pis;
            else
                return pis.GetRange(lastPI + 1, pis.Count - lastPI - 1);
        }

        public override string Name
        {
            get 
            {
                string propertiesName = properties.ToString(p => p.Name, TypeContext.Separator);
                return //((Parent.Name == TypeContext.Separator) ? "" : Parent.Name) +
                    Parent.Name +
                    (propertiesName.HasText() ? TypeContext.Separator + propertiesName : "");
            }
        }

        public override string FriendlyName
        {
            get { return LastProperty.NiceName(); }
        }

        public override void Dispose()
        {
            if (typeof(ImmutableEntity).IsAssignableFrom(typeof(T)))
                StyleContext.Current.Dispose();
        }
    }
    #endregion

    #region TypeElementContext<T>
    public class TypeElementContext<T> : TypeContext<T>, IDisposable
    {
        int Index;

        internal TypeContext Parent { get; private set; }

        public TypeElementContext(T value, TypeContext parent, int index)
            : base(value)
        {
            this.Index = index;
            Parent = parent;
        }

        public override PropertyInfo LastProperty
        {
            get { throw new NotImplementedException("TypeElementContext has no LastProperty"); }
        }

        public override Type LastIdentifiableProperty
        {
            get
            {
                if (Reflector.IsIdentifiableEntity(typeof(T)))
                    return typeof(T); 

                return Parent.LastIdentifiableProperty;
            }
        }

        public override List<PropertyInfo> GetPath()
        {
            List<PropertyInfo> pis = Parent.GetPath();

            Type lastIP = LastIdentifiableProperty;
            int lastPI = pis.FindLastIndex(pi => pi.PropertyType == lastIP);

            if (lastPI == -1)
                return pis;
            else
                return pis.GetRange(lastPI + 1, pis.Count - lastPI - 1);
        }

        public override string Name
        {
            get
            {
                return "";
            }
        }

        public override string FriendlyName
        {
            get { throw new NotImplementedException("TypeElementContext has no FriendlyName"); }
        }

        public override void Dispose()
        {
            if (typeof(ImmutableEntity).IsAssignableFrom(typeof(T)))
                StyleContext.Current.Dispose();
        }
    }
    #endregion
}
