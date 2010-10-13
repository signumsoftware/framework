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

namespace Signum.Web
{
    public class QuerySettings
    {
        public string Title { get; set; }
        public int? Top { get; set; }
        public string UrlName { get; set; }

        public Func<object, bool> IsFindable; 

        private Dictionary<string, Func<HtmlHelper, object, string>> formatters;
        public Dictionary<string, Func<HtmlHelper, object, string>> Formatters
        {
            get 
            {
                if (formatters == null)
                    formatters = new Dictionary<string, Func<HtmlHelper, object, string>>();
                return formatters;
            }
            set 
            {
                formatters = value;
            }
        }

        public static List<FormatterRule> FormatRules { get; set; }

        static QuerySettings()
        {
            FormatRules = new List<FormatterRule>
            {
                new FormatterRule(c=>true, c=> (h,o) =>
                {
                    return o != null ? o.ToString() : "";
                }),

                new FormatterRule(c => c.Type.UnNullify().IsEnum, c => (h,o) => 
                {
                    return o != null ? ((Enum)o).NiceToString() : "";
                }),
                new FormatterRule(c => c.Type.UnNullify().IsLite(), c => (h,o) => 
                {
                    return h.LightEntityLine((Lite)o, false);
                }),
                new FormatterRule(c=>c.Type.UnNullify() == typeof(DateTime), c => (h,o) => 
                {
                    return o != null ? ((DateTime)o).ToUserInterface().TryToString(c.Format) : "";
                }),
                new FormatterRule(c=> Reflector.IsNumber(c.Type), c => (h,o) => 
                {
                    if (o != null)
                    {
                        string s = ((IFormattable)o).TryToString(c.Format);
                        if (c.Unit.HasText())
                            s += " " + c.Unit;
                        return s;
                    }
                    return "";
                }),
                new FormatterRule(c=>c.Type == typeof(bool?), c => (h,o) => 
                {
                    return o != null ? "<div style='text-align:center'>"+h.CheckBox("", (bool)o, false)+"</div>" : "";
                }),
                new FormatterRule(c=>c.Type == typeof(bool), c => (h,o) => 
                {
                    return o != null ? "<div style='text-align:center'>"+h.CheckBox("", (bool)o, false)+"</div>" : "" ;
                })
            };
        }

        public Func<HtmlHelper, object, string> GetFormatter(Column column)
        {
            return formatters.TryGetC(column.Name) ??
                   FormatRules.Last(cfr => cfr.IsApplyable(column)).Formatter(column);
        }
    }

    public class FormatterRule
    {
        public Func<Column, Func<HtmlHelper, object, string>> Formatter { get; set; }
        public Func<Column, bool> IsApplyable { get; set; }

        public FormatterRule(Func<Column, bool> isApplyable, Func<Column, Func<HtmlHelper, object, string>> formatter)
        {
            Formatter = formatter;
            IsApplyable = isApplyable;
        }
    }
}
