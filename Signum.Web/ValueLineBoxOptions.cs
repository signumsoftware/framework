using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using System.Reflection;

namespace Signum.Web
{
    public class ValueLineBoxOptions
    {
        public string prefix;
        public ValueLineType type;
        public string title = SelectorMessage.ChooseAValue.NiceToString();
        public string message = SelectorMessage.PleaseChooseAValueToContinue.NiceToString();
        public string labelText = null;

        public object value;

        public ValueLineBoxOptions(ValueLineType type, string prefix)
        {
            this.type = type;
            this.prefix = prefix;
        }

        public ValueLineBoxOptions(ValueLineType type, string parentPrefix, string newPart)
            :this(type, "_".CombineIfNotEmpty(parentPrefix, newPart))
        {
        }
    }
}
