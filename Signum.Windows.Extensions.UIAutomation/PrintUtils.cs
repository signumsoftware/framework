using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Signum.Utilities;

namespace Signum.Windows.UIAutomation
{
    public static class PrintUtils
    {
        public static string NiceToString(this Condition condition)
        {
            {
                if (condition is PropertyCondition pc)
                    return "{0} = {1}".FormatWith(pc.Property.CleanPropertyName(), pc.Value?.ToString());
            }

            {
                if (condition is AndCondition ac)
                    return ac.GetConditions().ToString(c => c is PropertyCondition ? c.NiceToString() : "({0})".FormatWith(c.NiceToString()), " AND ");
            }

            {
                if (condition is OrCondition oc)
                    return oc.GetConditions().ToString(c => c is PropertyCondition ? c.NiceToString() : "({0})".FormatWith(c.NiceToString()), " OR ");
            }

            {
                if (condition is NotCondition nc)
                    return "NOT ({0})".FormatWith(nc.Condition.NiceToString());
            }
            throw new InvalidOperationException("{0} not expected".FormatWith(condition.GetType().Name));
        }


        static string CleanEnd(this string text, string postFix)
        {
            if (text.EndsWith(postFix))
                return text.Substring(0, text.Length - postFix.Length);

            return text;
        }

        public static string PrintPatterns(this AutomationElement ae)
        {
            return ae.GetSupportedPatterns().ToString(CleanPatternName, "\r\n");
        }

        public static string CleanPatternName(this AutomationPattern p)
        {
            return p.ProgrammaticName.CleanEnd("PatternIdentifiers.Pattern");
        }

        public static string PrintProperties(this AutomationElement ae)
        {
            return ae.GetSupportedProperties()
                .GroupBy(p => p.ProgrammaticName.Split('.')[0].CleanEnd("PatternIdentifiers"))
                .OrderBy(a => a.Key).ToString(gr => gr.Key + "\r\n" +
                    gr.Select(a => "  " + a.CleanPropertyName() + " = " + ae.GetCurrentPropertyValue(a)?.ToString())
                    .OrderBy()
                    .ToString("\r\n"),
                    "\r\n");
        }

        public static string CleanPropertyName(this AutomationProperty a)
        {
            return a.ProgrammaticName.Split('.')[1].CleanEnd("Property");
        }
    }
}
