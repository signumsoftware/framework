using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Entities;
using System.Web;
using System.Web.Mvc;

namespace Signum.Web
{
    public class QuerySettings
    {
        public string Title { get; set; }
        public int? Top { get; set; }
        public string UrlName { get; set; }

        private Dictionary<string, Action<HtmlHelper, object>> formatters;
        public Dictionary<string, Action<HtmlHelper, object>> Formatters
        {
            get 
            {
                if (formatters == null)
                    formatters = new Dictionary<string, Action<HtmlHelper, object>>();
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
                new FormatterRule(c=>true, (h,o) => h.Write(o.TryToString())),
                new FormatterRule(c=>typeof(Lazy).IsAssignableFrom(c.Type), (h,o)=>h.LightEntityLine((Lazy)o, false)),
            };
        }

        public Action<HtmlHelper, object> GetFormatter(Column column)
        {
            return formatters.TryGetC(column.Name) ??
                   FormatRules.Last(cfr => cfr.IsApplyable(column)).Formatter;
        }
    }

    public class FormatterRule
    {
        public Action<HtmlHelper, object> Formatter { get; set; }
        public Func<Column, bool> IsApplyable { get; set; }

        public FormatterRule(Func<Column, bool> isApplyable, Action<HtmlHelper, object> formatter)
        {
            Formatter = formatter;
            IsApplyable = isApplyable;
        }
    }
}
