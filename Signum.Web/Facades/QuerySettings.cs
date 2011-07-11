using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Entities;
using System.Web;
using System.Web.Mvc;
using Signum.Entities.Reflection;
using Signum.Web.Properties;
using System.Linq.Expressions;

namespace Signum.Web
{
    public class QuerySettings
    {
        public QuerySettings(object queryName)
        {
            this.QueryName = queryName; 
        }

        public object QueryName { get; private set; }

        public Func<string> Title { get; set; }
        public int? Top { get; set; }
        public string WebQueryName { get; set; }

        public Func<object, bool> IsFindable;

        public bool OnIsFindable()
        {
            if (IsFindable != null)
                foreach (Func<object, bool> item in IsFindable.GetInvocationList())
                {
                    if (!item(QueryName))
                        return false;
                }

            return true;
        }

        public static List<FormatterRule> FormatRules { get; set; }
        public static List<EntityFormatterRule> EntityFormatRules { get; set; }

        public static Dictionary<PropertyRoute, Func<HtmlHelper, object, MvcHtmlString>> PropertyFormatters { get; set; }

        Dictionary<string, Func<HtmlHelper, object, MvcHtmlString>> formatters;
        public Dictionary<string, Func<HtmlHelper, object, MvcHtmlString>> Formatters
        {
            get { return formatters ?? (formatters = new Dictionary<string, Func<HtmlHelper, object, MvcHtmlString>>()); }
            set { formatters = value; }
        }

        static QuerySettings()
        {
            FormatRules = new List<FormatterRule>
            {
                new FormatterRule(c=>true, c=> (h,o) =>
                {
                    return o != null ? o.ToString().EncodeHtml() : MvcHtmlString.Empty;
                }),

                new FormatterRule(c => c.Type.UnNullify().IsEnum, c => (h,o) => 
                {
                    return o != null ? ((Enum)o).NiceToString().EncodeHtml() : MvcHtmlString.Empty;
                }),
                new FormatterRule(c => c.Type.UnNullify().IsLite(), c => (h,o) => 
                {
                    return h.LightEntityLine((Lite)o, false);
                }),
                new FormatterRule(c=>c.Type.UnNullify() == typeof(DateTime), c => (h,o) => 
                {
                    return o != null ? ((DateTime)o).ToUserInterface().TryToString(c.Format).EncodeHtml() : MvcHtmlString.Empty;
                }),
                new FormatterRule(c=> Reflector.IsNumber(c.Type), c => (h,o) => 
                {
                    if (o != null)
                    {
                        string s = ((IFormattable)o).TryToString(c.Format);
                        if (c.Unit.HasText())
                            s += " " + c.Unit;
                        return s.EncodeHtml();
                    }
                    return MvcHtmlString.Empty;
                }),
                new FormatterRule(c=>c.Type == typeof(bool?), c => (h,o) => 
                {
                    return o != null ? AlignCenter(h.CheckBox("", (bool)o, false)) : MvcHtmlString.Empty;
                }),
                new FormatterRule(c=>c.Type == typeof(bool), c => (h,o) => 
                {
                    return o != null ? AlignCenter(h.CheckBox("", (bool)o, false)) : MvcHtmlString.Empty;
                })
            };

            EntityFormatRules = new List<EntityFormatterRule>
            {
                new EntityFormatterRule(l => true, (h,l) => 
                {
                    if (Navigator.IsViewable(l.RuntimeType, true))
                        return h.Href(Navigator.ViewRoute(l.RuntimeType, l.Id), h.Encode(Resources.View));
                    else
                        return MvcHtmlString.Empty;
                }),
            };

            PropertyFormatters = new Dictionary<PropertyRoute, Func<HtmlHelper, object, MvcHtmlString>>();
        }

        public static MvcHtmlString AlignCenter(MvcHtmlString innerHTML)
        {
            return new HtmlTag("div")
                .Attrs(new { style = "text-align:center" })
                .InnerHtml(innerHTML)
                .ToHtml();
        }
           

        public Func<HtmlHelper, object, MvcHtmlString> GetFormatter(Column column)
        {
            Func<HtmlHelper, object, MvcHtmlString> cf;
            if (formatters != null && formatters.TryGetValue(column.Name, out cf))
                return cf; 

            PropertyRoute route = column.Token.GetPropertyRoute();
            if (route != null)
            {
                var formatter = QuerySettings.PropertyFormatters.TryGetC(route);
                if (formatter != null)
                    return formatter;
            }

            return FormatRules.Last(cfr => cfr.IsApplyable(column)).Formatter(column);
        }


        public static void RegisterPropertyFormat<T>(Expression<Func<T, object>> property, Func<HtmlHelper, object, MvcHtmlString> formatter)
         where T : IRootEntity
        {
            PropertyFormatters.Add(PropertyRoute.Construct(property), formatter);
        }
    }

    public class FormatterRule
    {
        public Func<Column, Func<HtmlHelper, object, MvcHtmlString>> Formatter { get; set; }
        public Func<Column, bool> IsApplyable { get; set; }

        public FormatterRule(Func<Column, bool> isApplyable, Func<Column, Func<HtmlHelper, object, MvcHtmlString>> formatter)
        {
            Formatter = formatter;
            IsApplyable = isApplyable;
        }
    }

    public class EntityFormatterRule
    {
        public Func<HtmlHelper, Lite, MvcHtmlString> Formatter { get; set; }
        public Func<Lite, bool> IsApplyable { get; set; }

        public EntityFormatterRule(Func<Lite, bool> isApplyable, Func<HtmlHelper, Lite, MvcHtmlString> formatter)
        {
            Formatter = formatter;
            IsApplyable = isApplyable;
        }
    }
}
