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

        public bool WriteHiddenOnReadonly { get; set; }

        public ValueLine(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
        }

        protected override void SetReadOnly()
        {
        }

        public List<SelectListItem> CreateComboItems()
        {
            var items = new List<SelectListItem>();

            if (UntypedValue == null ||
                Type.IsNullable() && (PropertyRoute == null || !Validator.TryGetPropertyValidator(PropertyRoute).Validators.OfType<NotNullValidatorAttribute>().Any()))
            {
                items.Add(new SelectListItem() { Text = "-", Value = "" });
            }

            items.AddRange(Enum.GetValues(Type.UnNullify())
                .Cast<Enum>()
                .Select(v => new SelectListItem()
                {
                    Text = v.NiceToString(),
                    Value = v.ToString(),
                }));

            return items;
        }

        public bool InlineCheckbox { get; set; }
    }
}
