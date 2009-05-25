using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using Signum.Utilities;

namespace Signum.Windows
{
    public class DoubleListConverter : IMultiValueConverter
    {
        public static readonly DoubleListConverter Instance = new DoubleListConverter();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
                return null;

            List<ValidationError> validationErrors = (List<ValidationError>)values[0];

            string errors = values[1] as string;

            string[] entityErrors = errors.Lines();

            string[] cleanValidationErrors = validationErrors.Where(e => e.Exception != null ||
                !entityErrors.Contains(e.ErrorContent.ToString())).Select(e => e.ErrorContent.ToString()).ToArray();

            return cleanValidationErrors.Concat(entityErrors).ToArray();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
