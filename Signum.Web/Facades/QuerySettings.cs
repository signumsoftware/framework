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
using System.Linq.Expressions;
using System.Web.Mvc.Html;
using System.Globalization;
using Signum.Utilities.Reflection;

namespace Signum.Web
{
    public class QuerySettings
    {
        public object QueryName { get; private set; }

        public Pagination Pagination { get; set; }
        public string WebQueryName { get; set; }

        public bool IsFindable { get; set; }

        public string DefaultOrderColumn { get; set; }

        public QuerySettings(object queryName)
        {
            this.QueryName = queryName;
            this.IsFindable = true;
            this.DefaultOrderColumn = "Id";
        }

        public static List<FormatterRule> FormatRules { get; set; }
        public static List<EntityFormatterRule> EntityFormatRules { get; set; }

        public static Dictionary<PropertyRoute, CellFormatter> PropertyFormatters { get; set; }

        Dictionary<string, CellFormatter> formatters;
        public Dictionary<string, CellFormatter> Formatters
        {
            get { return formatters ?? (formatters = new Dictionary<string, CellFormatter>()); }
            set { formatters = value; }
        }

        public EntityFormatter EntityFormatter { get; set; }
        public RowAttributes RowAttributes { get; set; }
        public List<ColumnOption> HiddenColumns { get; set; }
        public Func<HtmlHelper, Context, QueryDescription, FindOptions, SimpleFilterBuilder> SimpleFilterBuilder { get; set; }

        static QuerySettings()
        {
            FormatRules = new List<FormatterRule>
            {
                new FormatterRule("object", c=>true, c=> new CellFormatter((h,o) =>
                {
                    return o != null ? o.ToString().EncodeHtml() : MvcHtmlString.Empty;
                }){ WriteData = false }),

                new FormatterRule("Enum", c => c.Type.UnNullify().IsEnum, c => new CellFormatter((h,o) => 
                {
                    return o != null ? ((Enum)o).NiceToString().EncodeHtml() : MvcHtmlString.Empty;
                })),

                new FormatterRule("Lite", c => c.Type.UnNullify().IsLite(), c => new CellFormatter((h,o) => 
                {
                    return h.LightEntityLine((Lite<IEntity>)o, isSearch: false);
                })),

                 new FormatterRule("Guid", c=>c.Type.UnNullify() == typeof(Guid), c => new CellFormatter((h,o) => 
                {
                    return o != null ? (new HtmlTag("span").Class("guid").SetInnerText(o.ToString().Start(5) + "…" + o.ToString().End(5))) : MvcHtmlString.Empty;
                }){ WriteData = true, TextAlign = "middle" }),

                new FormatterRule("DateTime", c=>c.Type.UnNullify() == typeof(DateTime), c => new CellFormatter((h,o) => 
                {
                    return o != null ? ((DateTime)o).ToUserInterface().ToString(c.Format).EncodeHtml() : MvcHtmlString.Empty;
                }){ WriteData = false, TextAlign = "right" }),

                new FormatterRule("TimeSpan",  c=>c.Type.UnNullify() == typeof(TimeSpan), c => new CellFormatter((h,o) => 
                {
                    return o != null ? ((TimeSpan)o).ToString(c.Format).EncodeHtml() : MvcHtmlString.Empty;
                }){ WriteData = false, TextAlign = "right" }),

                new FormatterRule("Number", c=> ReflectionTools.IsNumber(c.Type) && c.Unit == null, c => new CellFormatter((h,o) => 
                {
                    return o != null? ((IFormattable)o).ToString(c.Format, CultureInfo.CurrentCulture).EncodeHtml(): MvcHtmlString.Empty;
                }){ WriteData = false, TextAlign = "right" }),

                new FormatterRule("Number with Unit", c=> ReflectionTools.IsNumber(c.Type) && c.Unit.HasText(), c => new CellFormatter((h,o) => 
                {
                    if (o != null)
                    {
                        string s = ((IFormattable)o).ToString(c.Format, CultureInfo.CurrentCulture);
                        if (c.Unit.HasText())
                            s += " " + c.Unit;
                        return s.EncodeHtml();
                    }
                    return MvcHtmlString.Empty;
                }){ TextAlign = "right"}),

                new FormatterRule("bool", c=>c.Type.UnNullify() == typeof(bool), c => new CellFormatter((h,o) => 
                {
                    return o != null ? new HtmlTag("input")
                        .Attr("type", "checkbox")
                        .Attr("disabled", "disabled")
                        .Let(a => (bool)o ? a.Attr("checked", "checked") : a)
                        .ToHtml() : MvcHtmlString.Empty;
                }){ TextAlign = "center"}),
            };

            EntityFormatRules = new List<EntityFormatterRule>
            {
                new EntityFormatterRule(row => true, (h,row) => 
                {
                    if (Navigator.IsNavigable(row.Entity.EntityType, null, isSearch: true ))
                        return h.LightEntityLine(row.Entity, isSearch: true, innerText: EntityControlMessage.View.NiceToString());
                    else
                        return MvcHtmlString.Empty;
                }),
            };

            PropertyFormatters = new Dictionary<PropertyRoute, CellFormatter>();
        }

        public CellFormatter GetFormatter(Column column)
        {
            CellFormatter cf;
            if (formatters != null && formatters.TryGetValue(column.Name, out cf))
                return cf;

            PropertyRoute route = column.Token.GetPropertyRoute();
            if (route != null)
            {
                var formatter = QuerySettings.PropertyFormatters.TryGetC(route);
                if (formatter != null)
                    return formatter;
            }

            var last = FormatRules.Last(cfr => cfr.IsApplyable(column));

            return last.Formatter(column);
        }


        public static void RegisterPropertyFormat<T>(Expression<Func<T, object>> propertyRoute, CellFormatter formatter)
            where T : IRootEntity
        {
            PropertyFormatters.Add(PropertyRoute.Construct(propertyRoute), formatter);
        }
    }

    public class SimpleFilterBuilder
    {
        public MvcHtmlString Control { get; set; }
        public string Url { get; set; }

        public SimpleFilterBuilder(MvcHtmlString control, string url)
        {
            this.Control = control;
            this.Url = url;
        }
    }

    public class FormatterRule
    {
        public string Name { get; private set; }

        public Func<Column, CellFormatter> Formatter { get; set; }
        public Func<Column, bool> IsApplyable { get; set; }

        public FormatterRule(string name, Func<Column, bool> isApplyable, Func<Column, CellFormatter> formatter)
        {
            Name = name;
            IsApplyable = isApplyable;
            Formatter = formatter;
        }
    }

    public class EntityFormatterRule
    {
        public EntityFormatter Formatter { get; set; }
        public Func<ResultRow, bool> IsApplyable { get; set; }

        public EntityFormatterRule(Func<ResultRow, bool> isApplyable, EntityFormatter formatter)
        {
            Formatter = formatter;
            IsApplyable = isApplyable;
        }
    }

    public delegate MvcHtmlString EntityFormatter(HtmlHelper html, ResultRow lite);
    public delegate MvcHtmlString RowAttributes(HtmlHelper html, ResultRow row);

    public class CellFormatter
    {
        public bool WriteData = true;
        public string TextAlign;
        public Func<HtmlHelper, object, MvcHtmlString> Formatter;

        public CellFormatter(Func<HtmlHelper, object, MvcHtmlString> formatter)
        {
            this.Formatter = formatter;
        }

        public MvcHtmlString WriteDataAttribute(object value)
        {
            if(!WriteData)
                return MvcHtmlString.Empty;

            string key = value is Lite<Entity> ? ((Lite<Entity>)value).Key() : value?.ToString();

            return MvcHtmlString.Create("data-value=\"" + key + "\"");
        }
    }
}
