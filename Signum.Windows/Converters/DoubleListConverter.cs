using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using Signum.Utilities;
using System.Reflection;

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

            string[] cleanValidationErrors = validationErrors.Select(e => CleanErrorMessage(e)).ToArray();

            return cleanValidationErrors.Union(entityErrors).ToArray();
        }

        public static string CleanErrorMessage(ValidationError error)
        {
            if (error.Exception == null)
                return error.ErrorContent.ToString();

            if (error.Exception is TargetInvocationException)
                return error.Exception.InnerException.Message;

            return error.Exception.Message;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
