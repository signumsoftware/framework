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

        public static readonly Context Default = new Context(null, null)
        {
            FormGroupStyle = FormGroupStyle.Horizontal,
            LabelColumns = new BsColumn(2),
            ShowValidationMessage = true,
            ReadOnly = false,
        };

        FormGroupStyle? formGroupStyle;
        public FormGroupStyle FormGroupStyle
        {
            get { return formGroupStyle ?? Parent.FormGroupStyle; }
            set { formGroupStyle = value; }
        }

        BsColumn labelColummns;
        public BsColumn LabelColumns
        {
            get { return labelColummns ?? Parent.LabelColumns; }
            set 
            { 
                labelColummns = value; 
                valueColummns = value == null? null : value.Inverse();
            }
        }

        BsColumn valueColummns;
        public BsColumn ValueColumns
        {
            get { return valueColummns ?? Parent.ValueColumns; }
        }

        

        bool? readOnly; 
        public bool ReadOnly
        {
            get { return readOnly ?? Parent.ReadOnly; }
            set
            {
                readOnly = value;
                if (value) 
                    SetReadOnly();
            }
        }

        protected virtual void SetReadOnly() { }

        bool? showValidationMessage; 
        public bool ShowValidationMessage
        {
            get { return showValidationMessage ?? Parent.ShowValidationMessage; }
            set { showValidationMessage = value; }
        }

        public override string ToString()
        {
            return Prefix; 
        }

        public void Dispose()
        {
        }
    }


    public class BsColumn
    {
        public readonly short? xs;
        public readonly short? sm;
        public readonly short? md;
        public readonly short? lg;

        readonly string catchedString; 

        public BsColumn(short sm)
        {
            this.xs = null;
            this.sm = sm;
            this.md = null;
            this.lg = null;
            this.catchedString = "col-sm-" + sm;
        }

        public BsColumn(short? xs, short? sm, short? md, short? lg)
        {
            this.xs = xs;
            this.sm = sm;
            this.md = md;
            this.lg = lg;
            this.catchedString =  " ".CombineIfNotEmpty(
                xs == null? null: "col-xs-" + xs,
                sm == null ? null : "col-sm-" + sm,
                md == null ? null : "col-md-" + md,
                lg == null ? null : "col-lg-" + lg);
        }

        public BsColumn Inverse()
        {
            return new BsColumn(
                (short?)(12 - xs),
                (short?)(12 - sm),
                (short?)(12 - md),
                (short?)(12 - lg));
        }

        public override string ToString()
        {
            return catchedString;
        }
    }

    public enum FormGroupStyle
    {
        None,
        Basic,
        Inline,
        Horizontal,
    }

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
        List<Tab> ExpandTabs(List<Tab> tabs, string containerId, HtmlHelper helper, TypeContext context);
        MvcHtmlString OnSurroundLine(PropertyRoute propertyRoute, HtmlHelper helper, TypeContext tc, MvcHtmlString result);

    }

    public class ViewOverrides : IViewOverrides
    {
        Dictionary<string, Func<HtmlHelper, TypeContext, Tab>> beforeFieldset;
        public ViewOverrides BeforeTab(string id, Func<HtmlHelper, TypeContext, Tab> constructor)
        {
            if (beforeFieldset == null)
                beforeFieldset = new Dictionary<string, Func<HtmlHelper, TypeContext, Tab>>();

            beforeFieldset.Add(id, constructor);

            return this;
        }

        Dictionary<string, Func<HtmlHelper, TypeContext, Tab>> afterFieldset;
        public ViewOverrides AfterTab(string id, Func<HtmlHelper, TypeContext, Tab> constructor)
        {
            if (afterFieldset == null)
                afterFieldset = new Dictionary<string, Func<HtmlHelper, TypeContext, Tab>>();

            afterFieldset.Add(id, constructor);

            return this;
        }

        List<Tab> IViewOverrides.ExpandTabs(List<Tab> tabs, string containerId, HtmlHelper helper, TypeContext context)
        {
            List<Tab> newTabs = new List<Tab>();

            var before = beforeFieldset.TryGetC(containerId);
            if(before != null)
                Expand(before(helper, context), helper, context, newTabs); 

            foreach (var item in newTabs)
                Expand(item, helper, context, newTabs);

            var after = afterFieldset.TryGetC(containerId);
            if (after != null)
                Expand(after(helper, context), helper, context, newTabs); 

            return newTabs;
        }

        void Expand(Tab item, HtmlHelper helper, TypeContext context, List<Tab> newTabs)
        {
            var before = beforeFieldset.TryGetC(item.Id);
            if (before != null)
                Expand(before(helper, context), helper, context, newTabs);

            newTabs.Add(item);

            var after = afterFieldset.TryGetC(item.Id);
            if (after != null)
                Expand(after(helper, context), helper, context, newTabs);
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
