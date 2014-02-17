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
using Signum.Utilities.Reflection;
using Signum.Engine;
using Signum.Utilities.ExpressionTrees;
using System.IO;
using System.Web.WebPages;

namespace Signum.Web
{
    #region Context
    public class Context : IDisposable
    {
        public const string Separator = "_";

        public string Prefix { get; set; }

        public Context Parent { get; private set; } 

        public Context(Context parent, string prefix)
        {
            if(parent == null)
            {
                this.Parent = Default;
                this.Prefix = prefix; 
            }
            else
            {
                this.Parent = parent;
                this.Prefix = parent.Compose(prefix); 
            }
        }

        public string Compose(string nameToAppend)
        {
            return Prefix.Add(Separator, nameToAppend);
        }

        public string Compose(params string[] namesToAppend)
        {
            return Compose((IEnumerable<string>)namesToAppend);
        }

        public string Compose(IEnumerable<string> namesToAppend)
        {
            return this.Compose(namesToAppend.ToString(Separator));
        }

        #region Styles
        [Flags]
        enum BoolStyles : short
        {
            LabelVisible = 2,
            ShowValidationMessage = 4,
            ReadOnly =8,
            ValueFirst = 16,
            ShowFieldDiv =32,
            OnlyValue = 64
        }

        BoolStyles styleValues;
        BoolStyles styleHasValue;

        private bool? this[BoolStyles pos]
        {
            get
            {
                if ((styleHasValue & pos) == pos)
                    return (styleValues & pos) == pos;
                return null;
            }

            set
            {
                if (!value.HasValue)
                    styleHasValue &= ~pos;
                else
                {
                    styleHasValue |= pos;

                    if (value.Value)
                        styleValues |= pos;
                    else
                        styleValues &= ~pos;
                }
            }
        }

        public static readonly Context Default = new Context(null, null)
        {
            LabelVisible = true,
            ShowValidationMessage = true,
            ReadOnly = false,
            ValueFirst = false,
            OnlyValue = false
        };

        /* It prints only the value. Useful when used together with Html.Field helper
         * to join different valueLines in the same line */
        public bool OnlyValue
        {
            get { return this[BoolStyles.OnlyValue] ?? Parent.OnlyValue; }
            set { this[BoolStyles.OnlyValue] = value; }        
        }

        public bool LabelVisible
        {
            get { return this[BoolStyles.LabelVisible] ?? Parent.LabelVisible; }
            set { this[BoolStyles.LabelVisible] = value; }
        }

        public bool ReadOnly
        {
            get { return this[BoolStyles.ReadOnly] ?? Parent.ReadOnly; }
            set
            {
                this[BoolStyles.ReadOnly] = value;
                if (value) SetReadOnly();
            }
        }

        protected virtual void SetReadOnly() { }

        public bool ShowValidationMessage
        {
            get { return this[BoolStyles.ShowValidationMessage] ?? Parent.ShowValidationMessage; }
            set { this[BoolStyles.ShowValidationMessage] = value; }
        }

        public bool ValueFirst
        {
            get { return this[BoolStyles.ValueFirst] ?? Parent.ValueFirst; }
            set { this[BoolStyles.ValueFirst] = value; }
        }

        public override string ToString()
        {
            return Prefix; 
        }
        #endregion

        public void Dispose()
        {
        }
    }
    #endregion

    #region TypeContext
    public abstract class TypeContext : Context
    {
        IViewOverrides viewOverrides;
        public IViewOverrides ViewOverrides
        {
            get
            {
                if (viewOverrides != null)
                    return viewOverrides;

                TypeContext parent = Parent as TypeContext;

                if (parent != null)
                    return parent.ViewOverrides;

                return null;
            }
            set { viewOverrides = value; }
        }

        public abstract object UntypedValue { get; }

        public abstract Type Type { get; }

        public PropertyRoute PropertyRoute { get; private set; }

        protected TypeContext(Context parent, string prefix, PropertyRoute propertyRoute)
            :base(parent, prefix)
        {
            this.PropertyRoute = propertyRoute;
        }

        public RuntimeInfo RuntimeInfo()
        {
            if (this.UntypedValue == null)
                return null;

            Type type = this.UntypedValue.GetType();
            if (type.IsLite())
                return new RuntimeInfo((Lite<IIdentifiable>)this.UntypedValue);

            if (type.IsEmbeddedEntity())
                return new RuntimeInfo((EmbeddedEntity)this.UntypedValue);

            if (type.IsIdentifiableEntity())
                return new RuntimeInfo((IdentifiableEntity)this.UntypedValue);

            throw new ArgumentException("Invalid type {0} for RuntimeInfo. It must be Lite, IdentifiableEntity or EmbeddedEntity".Formato(type));
        }

        internal abstract TypeContext Clone(object newValue);
    }
    #endregion

    #region TypeContext<T>
    public class TypeContext<T> : TypeContext
    {
        public T Value { get; set; }

        public override object UntypedValue
        {
            get { return Value; }
        }

        public TypeContext(T value, string prefix)
            : base(null, prefix, PropertyRoute.Root(value.GetType()))
        {
            Value = value;
        }

        public TypeContext(T value, TypeContext parent, string prefix, PropertyRoute propertyRoute)
            : base(parent, prefix, propertyRoute)
        {
            Value = value;
        }

        public override Type Type
        {
            get { return typeof(T); }
        }


        public TypeContext<T> SubContext()
        {
            return new TypeContext<T>(this.Value, this, null, PropertyRoute);
        }

        public TypeContext<S> SubContext<S>(Expression<Func<T, S>> property)
        {
            return Common.WalkExpression(this, property);
        }

        public IEnumerable<TypeElementContext<S>> TypeElementContext<S>(Expression<Func<T, MList<S>>> property)
        {
            return TypeContextUtilities.TypeElementContext(Common.WalkExpression(this, property));
        }

        internal override TypeContext Clone(object newValue)
        {
            return new TypeContext<T>((T)newValue, (TypeContext)Parent, Prefix, PropertyRoute);
        }
    }
    #endregion

    #region TypeSubContext<T>
    public class TypeSubContext<T> : TypeContext<T>, IDisposable
    {
        PropertyInfo[] properties;

        public TypeSubContext(T value, TypeContext parent, PropertyInfo[] properties, PropertyRoute propertyRoute)
            : base(value, parent.ThrowIfNullC(""), properties.ToString(a => a.Name, Separator), propertyRoute)
        {
            this.properties = properties;
        }

        public PropertyInfo[] Properties
        {
            get { return properties; }
        }

        internal override TypeContext Clone(object newValue)
        {
            return new TypeSubContext<T>((T)newValue, (TypeContext)Parent, Properties, PropertyRoute);
        }
    }
    #endregion

    #region TypeElementContext<T>
    public class TypeElementContext<T> : TypeContext<T>
    {
        public int Index { get; private set; }

        public TypeElementContext(T value, TypeContext parent, int index)
            : base(value, parent, index.ToString(), parent.PropertyRoute.Add("Item"))
        {
            this.Index = index;
        }

        internal override TypeContext Clone(object newValue)
        {
            return new TypeElementContext<T>((T)newValue, (TypeContext)Parent, Index);
        }
    }
    #endregion

    public interface IViewOverrides
    {
        HelperResult OnSurrondFieldset(string id, HtmlHelper helper, TypeContext tc, HelperResult result);
        MvcHtmlString OnSurroundLine(PropertyRoute propertyRoute, HtmlHelper helper, TypeContext tc, MvcHtmlString result);
    }

    public class ViewOverrides : IViewOverrides
    {
        Dictionary<string, Func<HtmlHelper, TypeContext, MvcHtmlString>> beforeFieldset;
        public ViewOverrides BeforeFieldset(string id, Func<HtmlHelper, TypeContext, MvcHtmlString> constructor)
        {
            if (beforeFieldset == null)
                beforeFieldset = new Dictionary<string, Func<HtmlHelper, TypeContext, MvcHtmlString>>();

            beforeFieldset.Add(id, constructor);

            return this;
        }

        Dictionary<string, Func<HtmlHelper, TypeContext, MvcHtmlString>> afterFieldset;
        public ViewOverrides AfterFieldset(string id, Func<HtmlHelper, TypeContext, MvcHtmlString> constructor)
        {
            if (afterFieldset == null)
                afterFieldset = new Dictionary<string, Func<HtmlHelper, TypeContext, MvcHtmlString>>();

            afterFieldset.Add(id, constructor);

            return this;
        }

        HelperResult IViewOverrides.OnSurrondFieldset(string id, HtmlHelper helper, TypeContext tc, HelperResult result)
        {
            var before = beforeFieldset.TryGetC(id);
            var after = afterFieldset.TryGetC(id);

            if (before == null && after == null)
                return result;

            return new HelperResult(writer =>
            {
                if (before != null)
                {
                    var b = before(helper, tc);
                    if (!MvcHtmlString.IsNullOrEmpty(b))
                        writer.WriteLine(b);
                }

                result.WriteTo(writer);

                if (after != null)
                {
                    var a = after(helper, tc);
                    if (!MvcHtmlString.IsNullOrEmpty(a))
                        writer.WriteLine(a);
                }
            }); 
        }

        Dictionary<PropertyRoute, Func<HtmlHelper, TypeContext, MvcHtmlString>> beforeLine;
        public ViewOverrides BeforeLine<T, S>(Expression<Func<T, S>> propertyRoute, Func<HtmlHelper, TypeContext<T>, MvcHtmlString> constructor)
            where T : IRootEntity
        {
            return BeforeLine(PropertyRoute.Construct(propertyRoute), (helper, tc) => constructor(helper, (TypeContext<T>)tc));
        }

        public ViewOverrides BeforeLine(PropertyRoute propertyRoute, Func<HtmlHelper, TypeContext, MvcHtmlString> constructor)
        {
            if (beforeLine == null)
                beforeLine = new Dictionary<PropertyRoute, Func<HtmlHelper, TypeContext, MvcHtmlString>>();

            beforeLine.Add(propertyRoute, constructor);

            return this; 
        }


        Dictionary<PropertyRoute, Func<HtmlHelper, TypeContext, MvcHtmlString>> afterLine;
        public ViewOverrides AfterLine<T, S>(Expression<Func<T, S>> propertyRoute, Func<HtmlHelper, TypeContext<T>, MvcHtmlString> constructor)
            where T : IRootEntity
        {
            return AfterLine(PropertyRoute.Construct(propertyRoute), (helper, tc) => constructor(helper, (TypeContext<T>)tc));
        }

        public ViewOverrides AfterLine(PropertyRoute propertyRoute, Func<HtmlHelper, TypeContext, MvcHtmlString> constructor)
        {
            if (afterLine == null)
                afterLine = new Dictionary<PropertyRoute, Func<HtmlHelper, TypeContext, MvcHtmlString>>();

            afterLine.Add(propertyRoute, constructor);

            return this;
        }


        MvcHtmlString IViewOverrides.OnSurroundLine(PropertyRoute propertyRoute, HtmlHelper helper, TypeContext tc, MvcHtmlString result)
        {
            var before = beforeLine.TryGetC(propertyRoute);
            if (before != null)
                result = before(helper, tc).Concat(result);

            var after = afterLine.TryGetC(propertyRoute);
            if (after != null)
                result = result.Concat(after(helper, tc));

            return result;
        }

    }
}
