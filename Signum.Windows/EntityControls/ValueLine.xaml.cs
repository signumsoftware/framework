using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System.Reflection;
using System.ComponentModel;
using Signum.Utilities.DataStructures;
using System.Collections;
using System.Windows.Controls.Primitives;
using System.Globalization;
using Signum.Entities.Basics;

namespace Signum.Windows
{
    

    /// <summary>
    /// Utiliza una deduccion de propiedades muy agresiva:
    /// Value (binding) -> ValueType -> ValueLineType -> ValueControl
    /// </summary>
    public partial class ValueLine : LineBase
    {
        public static readonly DependencyProperty UnitTextProperty =
            DependencyProperty.Register("UnitText", typeof(string), typeof(ValueLine), new UIPropertyMetadata(null));
        public string UnitText
        {
            get { return (string)GetValue(UnitTextProperty); }
            set { SetValue(UnitTextProperty, value); }
        }

        public static readonly DependencyProperty FormatProperty =
         DependencyProperty.Register("Format", typeof(string), typeof(ValueLine), new UIPropertyMetadata(null));
        public string Format
        {
            get { return (string)GetValue(FormatProperty); }
            set { SetValue(FormatProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(ValueLine), new UIPropertyMetadata(null));
        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueLineTypeProperty =
            DependencyProperty.Register("ValueLineType", typeof(ValueLineType), typeof(ValueLine), new UIPropertyMetadata(ValueLineType.String));
        public ValueLineType ValueLineType
        {
            get { return (ValueLineType)GetValue(ValueLineTypeProperty); }
            set { SetValue(ValueLineTypeProperty, value); }
        }

        public static readonly DependencyProperty ValueControlProperty =
            DependencyProperty.Register("ValueControl", typeof(Control), typeof(ValueLine), new UIPropertyMetadata(null));
        public Control ValueControl
        {
            get { return (Control)GetValue(ValueControlProperty); }
            set { SetValue(ValueControlProperty, value); }
        }

        public ValueLine()
        {
            InitializeComponent();
        }

        public override void OnLoad(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            base.OnLoad(sender, e);

            if (this.NotSet(ValueLineTypeProperty))
                this.ValueLineType = Configurator.GetDefaultValueLineType(this.Type);

            this.ValueControl = this.CreateControl(this.ValueLineType, this.Type);

            this.label.Target = this.ValueControl;
        }

        protected internal override DependencyProperty CommonRouteValue()
        {
            return ValueProperty;
        }


        public static ValueLineConfigurator Configurator = new ValueLineConfigurator(); 

   
        private Control CreateControl(ValueLineType lineType, Type type)
        {
            Type nType = Nullable.GetUnderlyingType(type);
            bool nullable = nType != null;
            type = nType ?? type;
            Control control = Configurator.constructor[lineType](type, nullable, Format);
            if(Configurator.SetToolTipStyle(lineType, type, nullable))
              control.Style = (Style)FindResource("toolTip"); 
            
            Binding b; 
            BindingExpression bindingExpression = BindingOperations.GetBindingExpression(this, ValueProperty);
            if (bindingExpression != null) // is something is binded to ValueProperty, bind the new control to there
            {
                Binding binding = bindingExpression.ParentBinding;
                Validation.ClearInvalid(bindingExpression);
                BindingOperations.ClearBinding(this, ValueProperty);
                b = new Binding(binding.Path.Path)
                {
                    UpdateSourceTrigger =  Configurator.GetUpdateSourceTrigger(lineType, type, nullable, binding.UpdateSourceTrigger),
                    Mode = binding.Mode,
                    ValidatesOnExceptions = true,
                    ValidatesOnDataErrors = true,
                    NotifyOnValidationError = true,
                    Converter = binding.Converter,
                };
            }
            else //otherwise bind to value property
            {
                b = new Binding()
                {
                    Path = new PropertyPath(ValueLine.ValueProperty),
                    Source = this,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Mode = BindingMode.TwoWay,
                };
            }

            if (b.Converter == null)
                b.Converter = Configurator.GetConverter(lineType, type, nullable);

            ValidationRule validation = Configurator.GetValidation(lineType, type, nullable);
            if (validation != null)
                b.ValidationRules.Add(validation);

            DependencyProperty prop = Configurator.properties[lineType];
       
            control.SetBinding(prop, b);

            Binding rb = new Binding
            {
                Source = this,
                Path = new PropertyPath(Common.IsReadOnlyProperty),
                Mode = BindingMode.OneWay,
                Converter = Configurator.GetReadOnlyConverter(lineType, type, nullable)
            };

            control.SetBinding(Configurator.readOnlyProperties[lineType], rb);  
            // Binding b = new Binding(binding.Path.Path) { Mode = binding.Mode, UpdateSourceTrigger = binding.UpdateSourceTrigger };

            //System.Diagnostics.PresentationTraceSources.SetTraceLevel(b, PresentationTraceLevel.High);
   
            return control;
        }
    }

    public class ValueLineConfigurator
    {
        static DataTemplate comboDataTemplate;

        static ValueLineConfigurator()
        {
            Binding b = new Binding() { Mode = BindingMode.OneTime, Converter = Converters.EnumDescriptionConverter };
            comboDataTemplate = new DataTemplate
            {
                VisualTree = new FrameworkElementFactory(typeof(TextBlock))
                        .Do(f => f.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right))
                        .Do(f => f.SetBinding(TextBlock.TextProperty, b))
            };
        }


        public virtual ValueLineType GetDefaultValueLineType(Type type)
        {
            type = type.UnNullify();

            if (type.IsEnum)
                return ValueLineType.Enum;
            else if (type == typeof(Color))
                return ValueLineType.Color;
            else
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        return ValueLineType.Boolean;
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                    case TypeCode.Single:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return ValueLineType.Number;
                    case TypeCode.DateTime:
                        return ValueLineType.DateTime;
                    case TypeCode.Empty:
                    case TypeCode.Object:
                    case TypeCode.Char:
                    case TypeCode.String:
                    default:
                        return ValueLineType.String;
                }        
            }
        }

        public Dictionary<ValueLineType, DependencyProperty> properties = new Dictionary<ValueLineType, DependencyProperty>()
        {
            {ValueLineType.Enum, ComboBox.SelectedItemProperty},
            {ValueLineType.Boolean,CheckBox.IsCheckedProperty},
            {ValueLineType.Number, NumericTextBox.ValueProperty},
            {ValueLineType.String, TextBox.TextProperty},
            {ValueLineType.DateTime, DateTimePicker.SelectedDateProperty},
            {ValueLineType.Color, ColorPicker.SelectedColorProperty},
        };

        public Dictionary<ValueLineType, DependencyProperty> readOnlyProperties = new Dictionary<ValueLineType, DependencyProperty>()
        {
            {ValueLineType.Enum, ComboBox.IsEnabledProperty},
            {ValueLineType.Boolean,CheckBox.IsEnabledProperty},
            {ValueLineType.Number, NumericTextBox.IsReadOnlyProperty},
            {ValueLineType.String, TextBox.IsReadOnlyProperty},
            {ValueLineType.DateTime, DateTimePicker.IsReadOnlyProperty},
            {ValueLineType.Color, ColorPicker.IsReadOnlyProperty}
        };

        public Dictionary<ValueLineType, Func<Type, bool, string, Control>> constructor = new Dictionary<ValueLineType, Func<Type, bool, string, Control>>()
        {
            {ValueLineType.Enum, (t,n,f)=>new ComboBox(){ ItemsSource = GetEnums(t,n), ItemTemplate = comboDataTemplate, VerticalContentAlignment = VerticalAlignment.Center}},
            {ValueLineType.Boolean, (t,n,f)=>new CheckBox(){ VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left}},
            {ValueLineType.Number, (t,n,f)=>
            {
                var nt = new NumericTextBox(){ XIncrement = 10, YIncrement = 1};
                if(f != null)
                {
                    f = NullableDecimalConverter.NormalizeToDecimal(f);

                    nt.NullableDecimalConverter = 
                        f == NullableDecimalConverter.Integer.Format?  NullableDecimalConverter.Integer:
                        f == NullableDecimalConverter.Number.Format?  NullableDecimalConverter.Number:
                        new NullableDecimalConverter(f); 
                }
                return nt;
            }},
            {ValueLineType.String, (t,n,f)=> new TextBox()},
            {ValueLineType.DateTime, (t,n,f)=> 
            {
                var dt = new DateTimePicker(); 
                if(f!= null) 
                {
                    dt.DateTimeConverter = 
                        f == DateTimeConverter.DateAndTime.Format?  DateTimeConverter.DateAndTime:
                        f == DateTimeConverter.Date.Format?  DateTimeConverter.Date:
                        new DateTimeConverter(f); 
                }
                return dt;
            }},
            {ValueLineType.Color, (t,n,f) => new ColorPicker()}
        };       


        public virtual IValueConverter GetConverter(ValueLineType lineType, Type type, bool nullable)
        {
            if (lineType == ValueLineType.Enum && nullable)
                return Converters.NullableEnumConverter;

            if (lineType == ValueLineType.Color)
                return Converters.ColorConverter;

            if (nullable)
                return Converters.Identity;

            return null;
        }


        public virtual ValidationRule GetValidation(ValueLineType lineType, Type type, bool nullable)
        {
            if (!nullable && type.IsValueType)
                return NotNullValidationRule.Instance;

            return null;
        }

        public virtual bool SetToolTipStyle(ValueLineType lineType, Type type, bool nullable)
        {
            if (lineType == ValueLineType.String)
                return false;

            return true; 
        }

        public virtual UpdateSourceTrigger GetUpdateSourceTrigger(ValueLineType lineType, Type type, bool nullable, UpdateSourceTrigger original)
        {
            return UpdateSourceTrigger.PropertyChanged;
        }

        public virtual IValueConverter GetReadOnlyConverter(ValueLineType lineType, Type type, bool nullable)
        {
            if (lineType == ValueLineType.Boolean || lineType == ValueLineType.Enum)
                return Converters.Not;

            return null;
        }

        public static IEnumerable GetEnums(Type t, bool b)
        {
            var bla = Enum.GetValues(t).Cast<object>();
            if (b)
                bla = bla.PreAnd("-");
            return bla.ToArray();
        }
    }

    public enum ValueLineType
    {
        Enum,
        Boolean,
        Number,
        String,
        DateTime,
        Color
    };
}
