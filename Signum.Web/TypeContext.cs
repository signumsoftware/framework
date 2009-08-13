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

namespace Signum.Web
{
    #region TypeContextHelper
    public static class TypeContextHelper
    {
        public static TypeContext<T> TypeContext<T>(this HtmlHelper helper)
        {
            if (helper.ViewData.Model is TypeContext<T>)
                return (TypeContext<T>)helper.ViewData.Model;

            return helper.BeginContext<T>((T)helper.ViewData.Model, null);
        }

        public static TypeContext<T> TypeContext<T>(this HtmlHelper helper, string viewDataKeyAndPrefix)
        {
            if (!viewDataKeyAndPrefix.HasText())
                return TypeContext<T>(helper);

            return helper.BeginContext<T>((T)helper.ViewData[viewDataKeyAndPrefix], viewDataKeyAndPrefix);
        }

        static TypeContext<T> BeginContext<T>(this HtmlHelper helper, T value, string prefix)
        {
            TypeContext<T> tc = new TypeContext<T>(value, prefix);

            if (typeof(IdentifiableEntity).IsAssignableFrom(typeof(T)))
            {
                IdentifiableEntity id = (IdentifiableEntity)(object)value;

                if (!helper.IsContainedEntity())
                {
                    helper.ViewContext.HttpContext.Response.Write(
                        helper.Hidden(helper.GlobalName(Signum.Web.TypeContext.Separator + Signum.Web.TypeContext.RuntimeType), typeof(T).Name) + "\n");
                    helper.ViewContext.HttpContext.Response.Write(
                        helper.Hidden(helper.GlobalName(Signum.Web.TypeContext.Separator + Signum.Web.TypeContext.Id), id.TryCS(i => i.IdOrNull)) + "\n");
                }
            }
            else if (typeof(EmbeddedEntity).IsAssignableFrom(typeof(T)))
            {
                helper.ViewContext.HttpContext.Response.Write(
                        helper.Hidden(helper.GlobalName(Signum.Web.TypeContext.Separator + Signum.Web.TypeContext.RuntimeType), typeof(T).Name) + "\n");
            }

            return tc;
        }

        public static TypeContext<S> TypeContext<T, S>(this HtmlHelper helper, TypeContext<T> parent, Expression<Func<T, S>> property)
        {
            Expression<Func<T, object>> expression = Expression.Lambda<Func<T, object>>(property.Body, property.Parameters);
            return (TypeContext<S>)Common.WalkExpression(parent, expression);
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
          
        public const string CssLineLabel = "labelLine";

        public abstract object UntypedValue { get; }
        public abstract string Name { get; }

        public abstract string FriendlyName { get; }
        public abstract Type ContextType { get; }
        public abstract List<PropertyInfo> GetPath();
        public abstract PropertyInfo Property { get; }

        public virtual void Dispose()
        {
            //Do nothing
        }

        static Dictionary<Type, Func<object, TypeContext, PropertyInfo, TypeContext>> typeSubContextCache = new Dictionary<Type, Func<object, TypeContext, PropertyInfo, TypeContext>>();

        internal static TypeContext Create(Type type, object value, TypeContext parent, PropertyInfo pi)
        {
            Func<object, TypeContext, PropertyInfo, TypeContext> constructor = null;
            lock (typeSubContextCache)
                constructor = typeSubContextCache.GetOrCreate(type, () =>
                {
                    ParameterExpression peO = Expression.Parameter(typeof(object), "o");
                    ParameterExpression peTC = Expression.Parameter(typeof(TypeContext), "tc");
                    ParameterExpression pePI = Expression.Parameter(typeof(PropertyInfo), "pi");
                    return Expression.Lambda<Func<object, TypeContext, PropertyInfo, TypeContext>>(
                            Expression.New(
                             typeof(TypeSubContext<>).MakeGenericType(type).GetConstructor(new[] { type, typeof(TypeContext), typeof(PropertyInfo) }),
                             Expression.Convert(peO, type), peTC, pePI),
                            peO, peTC, pePI).Compile();
                });

            return constructor(value, parent, pi);
        }
    }
    #endregion

    #region TypeContext<T>
    public class TypeContext<T> : TypeContext
    {
        public T Value { get; private set; }
        string prefix;

        public override List<PropertyInfo> GetPath() 
        {
            return new List<PropertyInfo>();
        }

        public override object UntypedValue
        {
            get { return Value; }
        }

        internal TypeContext(T value)
        {
            Value = value;
        }

        internal TypeContext(T value, string prefix)
        {
            Value = value;
            this.prefix = prefix; 
        }

        public override string Name
        {
            get { return  TypeContext.Separator + prefix; }
        }

        public override string FriendlyName
        {
            get { throw new NotImplementedException("TypeContext has no DisplayName"); }
        }

        public override PropertyInfo Property
        {
	         get { throw new NotImplementedException("TypeContext has no Property"); }
        }

        public override Type ContextType
        {
            get { return typeof(T); }
        }
    }
    #endregion

    #region TypeSubContext<T>
    internal class TypeSubContext<T> : TypeContext<T>, IDisposable
    {
        PropertyInfo property; 
        internal TypeContext Parent { get; private set; }

        public TypeSubContext(T value, TypeContext parent, PropertyInfo property)
            : base(value)
        {
            this.property = property;
            Parent = parent;
        }

        public override PropertyInfo Property
        {
	         get { return property; }
        }        

        public override List<PropertyInfo> GetPath()
        {
            return Parent.GetPath().Do(l => l.Add(Property));
        }

        public override string Name
        {
            get { return ((Parent.Name == TypeContext.Separator) ? "" : Parent.Name) + TypeContext.Separator + Property.Name; }
        }

        public override string FriendlyName
        {
            get { return Property.NiceName(); }
        }
    }
    #endregion
}
