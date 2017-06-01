using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using System.Reflection;
using Signum.Utilities;

namespace Signum.Windows.UIAutomation
{
    public static class PatternExtensions
    {
        static readonly Dictionary<Type, AutomationPattern> patterns = new Dictionary<Type, AutomationPattern>();

        public static P Pattern<P>(this AutomationElement ae) where P : BasePattern
        {
            AutomationPattern key = GetAutomationPatternKey(typeof(P));

            return (P)ae.GetCurrentPattern(key);
        }

        public static P TryPattern<P>(this AutomationElement ae) where P : BasePattern
        {
            AutomationPattern key = GetAutomationPatternKey(typeof(P));

            if (ae.GetSupportedPatterns().Contains(key))
                return (P)ae.GetCurrentPattern(key);

            return null;
        }

        private static AutomationPattern GetAutomationPatternKey(Type patternType)
        {
            AutomationPattern key = patterns.GetOrCreate(patternType, () =>
                (AutomationPattern)patternType.GetField("Pattern", BindingFlags.Static | BindingFlags.Public).GetValue(null));
            return key;
        }

    }
}
