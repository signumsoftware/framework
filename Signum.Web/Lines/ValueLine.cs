using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Entities;
using System.Web.Routing;
using Signum.Utilities;

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

        public string RadioButtonLabelTrue = EntityControlMessage.Yes.NiceToString();
        public string RadioButtonLabelFalse = EntityControlMessage.No.NiceToString();

        public bool WriteHiddenOnReadonly { get; set; }

        public ValueLine(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
        }

        protected override void SetReadOnly()
        {
        }
    }
}
