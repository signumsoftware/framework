using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;
using Signum.Entities.DynamicQuery;
using System.Windows.Controls;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Reflection;
using System.Globalization;
using System.Windows.Media;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;

namespace Signum.Windows
{
    public class QuerySettings
    {
        public object QueryName { get; private set; }

        public ImageSource Icon { get; set; }
        public Pagination Pagination { get; set; }

        public QuerySettings(object queryName)
        {
            this.QueryName = queryName;
            this.IsFindable = true;
        }

        public bool IsFindable { get; set; }
    
        public static Dictionary<PropertyRoute, Func<Binding, DataTemplate>> PropertyFormatters { get; set; }
        public static List<FormatterRule> FormatRules { get; set; }

        Dictionary<string, Func<Binding, DataTemplate>> formatters;
        public Dictionary<string, Func<Binding, DataTemplate>> Formatters
        {
            get { return formatters ?? (formatters = new Dictionary<string, Func<Binding, DataTemplate>>()); }
            set { formatters = value; }
        }

        static QuerySettings()
        {
            FormatRules = new List<FormatterRule>
            {
                new FormatterRule("Default", c=>true, c=> b => FormatTools.TextBlockTemplate(b, TextAlignment.Left, null)),

                new FormatterRule("Checkbox", c=>c.Type.UnNullify() == typeof(bool), c=> b => FormatTools.CheckBoxTemplate(b, c.Format == null ? null : ConverterFactory.New(Reflector.GetPropertyFormatter(c.Format, null)))),

                new FormatterRule("Enum", c=>c.Type.UnNullify().IsEnum, c=> b => FormatTools.TextBlockTemplate(b, TextAlignment.Left, LocalizedAssembly.GetDescriptionOptions(c.Type.UnNullify()).IsSet(DescriptionOptions.Members) ? Converters.EnumDescription: null)),
                new FormatterRule("Number", c=> ReflectionTools.IsNumber(c.Type), c => b => FormatTools.TextBlockTemplate(b, TextAlignment.Right, c.Format == null ? null : ConverterFactory.New(Reflector.GetPropertyFormatter(c.Format, null)))),
                new FormatterRule("DateTime", c=>c.Type.UnNullify() == typeof(DateTime), c => b => FormatTools.TextBlockTemplate(b, TextAlignment.Right, c.Format == null ? null : ConverterFactory.New(Reflector.GetPropertyFormatter(c.Format, null)))),    
                new FormatterRule("TimeSpan", c=>c.Type.UnNullify() == typeof(TimeSpan), c => b => FormatTools.TextBlockTemplate(b, TextAlignment.Right, c.Format == null ? null : ConverterFactory.New(Reflector.GetPropertyFormatter(c.Format, null)))),
                new FormatterRule("Lite", c=>c.Type.IsLite(), //Not on entities! 
                    c=> b=> FormatTools.LightEntityLineTemplate(b)),

                new FormatterRule("NumberUnit", c=> ReflectionTools.IsNumber(c.Type) && c.Unit != null, c => b => FormatTools.TextBlockTemplate(b, TextAlignment.Right, ConverterFactory.New(Reflector.GetPropertyFormatter(c.Format,c.Unit))))
            };

            PropertyFormatters = new Dictionary<PropertyRoute, Func<Binding, DataTemplate>>();
        }

        public static void RegisterPropertyFormat<T>(Expression<Func<T, object>> property, Func<Binding, DataTemplate> formatter)
            where T : IRootEntity
        {
            PropertyFormatters.Add(PropertyRoute.Construct(property), formatter);
        }

        public Func<Binding, DataTemplate> GetFormatter(Column column)
        {
            if (formatters != null && formatters.TryGetValue(column.Name, out Func<Binding, DataTemplate> cf))
                return cf;

            PropertyRoute route = column.Token.GetPropertyRoute();
            if (route != null)
            {
                var formatter = QuerySettings.PropertyFormatters.TryGetC(route);
                if (formatter != null)
                    return formatter;
            }

            FormatterRule fr = FormatRules.Last(cfr => cfr.IsApplicable(column));

            return fr.Formatter(column);
        }

        public Func<QueryDescription, ISimpleFilterBuilder> SimpleFilterBuilder;

        
    }

    public class FormatterRule
    {
        public string Name { get; private set; }

        public Func<Column, Func<Binding, DataTemplate>> Formatter { get; set; }
        public Func<Column, bool> IsApplicable { get; set; }

        public FormatterRule(string name, Func<Column, bool> isApplicable, Func<Column, Func<Binding, DataTemplate>> formatter)
        {
            Name = name;
            IsApplicable = isApplicable;
            Formatter = formatter;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public static class FormatTools
    {
        public static DataTemplate TextBlockTemplate(Binding binding, TextAlignment alignment, IValueConverter converter)
        {
            if (converter != null)
                binding.Converter = converter;

            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(TextBlock));

            if (alignment != TextAlignment.Left)
                factory.SetValue(TextBlock.TextAlignmentProperty, alignment);
            factory.SetBinding(TextBlock.TextProperty, binding);
            return new DataTemplate { VisualTree = factory };
        }

        public static DataTemplate CheckBoxTemplate(Binding binding, IValueConverter converter)
        {
            if (converter != null)
                binding.Converter = converter;

            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(CheckBox));

            factory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            factory.SetValue(CheckBox.IsEnabledProperty, false);
            factory.SetBinding(CheckBox.IsCheckedProperty, binding);
            return new DataTemplate { VisualTree = factory };
        }

        public static DataTemplate LightEntityLineTemplate(Binding b)
        {
            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(LightEntityLine));
            factory.SetBinding(LightEntityLine.EntityProperty, b);
            factory.SetValue(LightEntityLine.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            return new DataTemplate { VisualTree = factory };
        }
    }
}
