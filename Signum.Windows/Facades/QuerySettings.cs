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

namespace Signum.Windows
{
    public class QuerySettings
    {
        public int? Top { get; set; }
        public ImageSource Icon { get; set; }

        private Dictionary<string, Func<Binding, DataTemplate>> formatters;
        public Dictionary<string, Func<Binding, DataTemplate>> Formatters
        {
            get
            {
                if (formatters == null)
                    formatters = new Dictionary<string, Func<Binding, DataTemplate>>();
                return formatters;
            }
            set
            {
                formatters = value;
            }
        }

        public static List<FormatterRule> FormatRules { get; set; }

        static QuerySettings()
        {
            FormatRules = new List<FormatterRule>
            {
                new FormatterRule(FormatterPriority.Default, "Default",
                    c=>true, 
                    c=> b => FormatTools.TextBlockTemplate(b, TextAlignment.Left, null)),

                new FormatterRule(FormatterPriority.Type, "Checkbox",
                    c=>c.Type.UnNullify() == typeof(bool), 
                    c=> b => FormatTools.CheckBoxTemplate(b, c.Format == null ? null : ConverterFactory.New(Reflector.GetPropertyFormatter(c.Format, null)))),

                    new FormatterRule(FormatterPriority.Type, "Enum",
                    c=>c.Type.IsEnum, 
                    c=> b => FormatTools.TextBlockTemplate(b, TextAlignment.Left, Converters.EnumDescriptionConverter)),
                new FormatterRule(FormatterPriority.Type, "Number",
                    c=>Reflector.IsNumber(c.Type), 
                    c => b => FormatTools.TextBlockTemplate(b, TextAlignment.Right, c.Format == null ? null : ConverterFactory.New(Reflector.GetPropertyFormatter(c.Format, null)))),
                new FormatterRule(FormatterPriority.Type, "DateTime",
                    c=>c.Type.UnNullify() == typeof(DateTime), 
                    c => b => FormatTools.TextBlockTemplate(b, TextAlignment.Right, c.Format == null ? null : ConverterFactory.New(Reflector.GetPropertyFormatter(c.Format, null)))),
                new FormatterRule(FormatterPriority.Type, "Lazy",
                    c=>typeof(Lazy).IsAssignableFrom(c.Type), //Not on entities! 
                    c=> b=> FormatTools.LightEntityLineTemplate(b)),

                new FormatterRule(FormatterPriority.Property, "NumberUnit",
                    c=>Reflector.IsNumber(c.Type) && c.TwinProperty != null,
                    c => b => FormatTools.TextBlockTemplate(b, TextAlignment.Right, ConverterFactory.New(Reflector.GetPropertyFormatter(c.Format,c.TwinProperty.SingleAttribute<UnitAttribute>().TryCC(u=>u.UnitName)))))
            }; 
        }

        public Func<Binding, DataTemplate> GetFormatter(Column column)
        {
            var result = formatters.TryGetC(column.Name);
            if(result != null)
                return result;

            FormatterRule fr = FormatRules.Where(cfr => cfr.IsApplyable(column)).WithMax(a=>a.Priority); 

            return fr.Formatter(column); 
        }
    }

    public class FormatterPriority
    {
        public const int Default = 0;
        public const int Type = 100;
        public const int Property = 200;
    }

    public class FormatterRule
    {
        public int Priority { get; set; }
        public string Name { get; set; }

        public Func<Column, Func<Binding, DataTemplate>> Formatter { get; set; }
        public Func<Column, bool> IsApplyable { get; set; }

        public FormatterRule(int priority, string name, Func<Column, bool> isApplyable, Func<Column, Func<Binding, DataTemplate>> formatter)
        {
            Priority = priority;
            Name = name;
            IsApplyable = isApplyable;
            Formatter = formatter;
        }

        public override string ToString()
        {
            return "{0} ({1})".Formato(Name, Priority);
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
