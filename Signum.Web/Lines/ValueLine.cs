using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Entities;
using System.Web.Routing;
using Signum.Web.Properties;

namespace Signum.Web
{
    public class ValueLine : BaseLine
    {
        public readonly RouteValueDictionary ValueHtmlProps = new RouteValueDictionary();

        public ValueLineType? ValueLineType { get; set; }
        public string Format { get; set; }
        public string UnitText { get; set; }
                
        public List<SelectListItem> EnumComboItems { get; set; }
        public DatePickerOptions DatePickerOptions { get; set; }

        public string RadioButtonLabelTrue = Resources.Yes;
        public string RadioButtonLabelFalse = Resources.No;

        public bool WriteHiddenOnReadonly { get; set; }

        public ValueLine(Type type, object untypedValue, Context parent, string controlID, FieldRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
        }

        protected override void SetReadOnly()
        {
        }
    }
}
