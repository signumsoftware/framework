using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows;

namespace Signum.Windows
{
    public static class ConverterFactory
    {
        public static Converter<S, T> New<S, T>(Func<S, T> convert)
        {
            return new Converter<S, T> { convert = convert };
        }

        public static DualConvertet<S1, S2, T> New<S1, S2, T>(Func<S1, S2, T> convert)
        {
            return new DualConvertet<S1, S2, T> { convert = convert };
        }

        public static MultiConverter<S, T> NewMulti<S, T>(Func<S[], T> convert)
        {
            return new MultiConverter<S, T> { convert = convert };
        }

        public static Converter<S, T> New<S, T>(Func<S, T> convert, Func<T, S> convertBack)
        {
            return new Converter<S, T> { convert = convert, convertBack = convertBack };
        }

        public static ConverterValidator<S, T> New<S, T>(Func<S, T> convert, Func<T, S> convertBack, Func<S, string> validator)
        {
            return new ConverterValidator<S, T> { convert = convert, convertBack = convertBack, validator = validator };
        }
    }

    public static class BindingExtensions
    {
        public static Binding AddConverterValidator<T>(this Binding binding, T validationConverter) where T : ValidationRule, IValueConverter
        {
            binding.ValidationRules.Add(validationConverter);
            binding.Converter = validationConverter;
            return binding;
        }

        public static Binding AddValidation(this Binding binding, ValidationRule validationRule)
        {
            binding.ValidationRules.Add(validationRule);
            return binding;
        }
    }


    public class Converter<S, T> : IValueConverter
    {
        internal Func<S, T> convert;
        internal Func<T, S> convertBack;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (convert == null)
                throw new NotImplementedException();

            T result = convert((S)value);

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (convertBack == null)
                throw new NotImplementedException();

            S result = convertBack((T)value);

            return result;
        }
    }

    public class DualConvertet<S1, S2, T> : IMultiValueConverter
    {
        internal Func<S1, S2, T> convert;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (convert == null)
                throw new NotImplementedException();

            if (values == null || values.Length != 2 || (values[0] == DependencyProperty.UnsetValue) || (values[1] == DependencyProperty.UnsetValue))
                return default(T);

            T result = convert((S1)values[0], (S2)values[1]);

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MultiConverter<S, T> : IMultiValueConverter
    {
        internal Func<S[], T> convert;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (convert == null)
                throw new NotImplementedException();

            if (values.Contains(DependencyProperty.UnsetValue))
                return DependencyProperty.UnsetValue;

            T result = convert(values.Cast<S>().ToArray());

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ConverterValidator<S, T> : ValidationRule, IValueConverter
    {
        internal Func<S, T> convert;
        internal Func<T, S> convertBack;
        internal Func<S, string> validator;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (this.convert == null)
            {
                throw new NotImplementedException();
            }
            return this.convert((S)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (this.convertBack == null)
            {
                throw new NotImplementedException();
            }
            return this.convertBack((T)value);
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string error = null;
            if (this.validator != null)
            {
                error = this.validator((S)value);
            }
            return new ValidationResult(error == null, error);
        }
    }

    public class PipeExtension : MarkupExtension, IValueConverter
    {
        public IValueConverter First { get; set; }
        public IValueConverter Second { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object temp = First.Convert(value, null, null, culture);
            object result = Second.Convert(temp, targetType, null, culture);
            return result; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object temp = Second.ConvertBack(value, null, null, culture);
            object result = First.ConvertBack(temp, targetType, null, culture);
            return result; 
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }


}
