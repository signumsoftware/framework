using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Globalization;
using Signum.Windows.Properties;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Windows
{
    public class NotNullValidationRule : ValidationRule
    {
        public static readonly NotNullValidationRule Instance = new NotNullValidationRule();

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value == null)
                return new ValidationResult(false, EntityControlMessage.NullValueNotAllowed.NiceToString());
            else
                return new ValidationResult(true, null);
        }
    }
}
