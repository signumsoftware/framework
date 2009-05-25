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
    public partial class ValueLine : UserControl
    {
        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register("LabelText", typeof(string), typeof(ValueLine), new UIPropertyMetadata("Property"));
        public string LabelText
        {
            get { return (string)GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(ValueLine), new UIPropertyMetadata(null));
        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueTypeProperty =
            DependencyProperty.Register("ValueType", typeof(Type), typeof(ValueLine), new UIPropertyMetadata(null));
        public Type ValueType
        {
            get { return (Type)GetValue(ValueTypeProperty); }
            set { SetValue(ValueTypeProperty, value); }
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
            Loaded += new RoutedEventHandler(ValueLine_Loaded);
        }

        void ValueLine_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (this.ValueControl == null)
            {
                if (this.ValueType == null)
                    throw new ApplicationException(Properties.Resources.TypeForValueLineNotDetermined.Formato(this.LabelText));

                if (this.NotSet(ValueLineTypeProperty))
                    this.ValueLineType = Configurator.GetDefaultValueLineType(this.ValueType);

                this.ValueControl = this.CreateControl(this.ValueLineType, this.ValueType);
            }
            this.label.Target = this.ValueControl;
        }

        public static ValueLineConfigurator Configurator = new ValueLineConfigurator(); 

   
        private Control CreateControl(ValueLineType lineType, Type type)
        {
            Type nType = Nullable.GetUnderlyingType(type);
            bool nullable = nType != null;
            type = nType ?? type;
            Control control = Configurator.constructor[lineType](type, nullable);
            control.Style = (Style)FindResource("toolTip"); 

            BindingExpression bindingExpression = BindingOperations.GetBindingExpression(this, ValueProperty);
            Binding binding = bindingExpression.ParentBinding;
            Validation.ClearInvalid(bindingExpression);
            BindingOperations.ClearBinding(this, ValueProperty);

            DependencyProperty prop = Configurator.properties[lineType];

            Binding b = new Binding(binding.Path.Path)
            {
                Converter = Configurator.GetConverter(lineType, type, nullable),
                UpdateSourceTrigger = binding.UpdateSourceTrigger,
                Mode = binding.Mode,
                ValidatesOnExceptions = true,
                ValidatesOnDataErrors = true,
                NotifyOnValidationError= true,
            };

            ValidationRule validation = Configurator.GetValidation(lineType, type, nullable);
            if (validation != null)
                b.ValidationRules.Add(validation); 
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
        public virtual ValueLineType GetDefaultValueLineType(Type type)
        {
            type = type.UnNullify();

            if (type.IsEnum)
                return ValueLineType.Enum;
            else if (type == typeof(ColorDN))
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
                        return ValueLineType.DecimalNumber;
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
            {ValueLineType.DecimalNumber, NumericTextBox.ValueProperty},
            {ValueLineType.Currency, NumericTextBox.ValueProperty},
            {ValueLineType.HighPrecisionNumber, NumericTextBox.ValueProperty},
            {ValueLineType.String, TextBox.TextProperty},
            {ValueLineType.DateTime, DateTimePicker.SelectedDateProperty},
            {ValueLineType.Date, DateTimePicker.SelectedDateProperty},
            {ValueLineType.Color, ColorPicker.SelectedColorProperty},
        };

        public Dictionary<ValueLineType, DependencyProperty> readOnlyProperties = new Dictionary<ValueLineType, DependencyProperty>()
        {
            {ValueLineType.Enum, ComboBox.IsEnabledProperty},
            {ValueLineType.Boolean,CheckBox.IsEnabledProperty},
            {ValueLineType.Number, NumericTextBox.IsReadOnlyProperty},
            {ValueLineType.DecimalNumber, NumericTextBox.IsReadOnlyProperty},
            {ValueLineType.Currency, NumericTextBox.IsReadOnlyProperty},
            {ValueLineType.HighPrecisionNumber, NumericTextBox.IsReadOnlyProperty},
            {ValueLineType.String, TextBox.IsReadOnlyProperty},
            {ValueLineType.DateTime, DateTimePicker.IsReadOnlyProperty},
            {ValueLineType.Date, DateTimePicker.IsReadOnlyProperty},
            {ValueLineType.Color, ColorPicker.IsReadOnlyProperty}
        };

        public Dictionary<ValueLineType, Func<Type, bool, Control>> constructor = new Dictionary<ValueLineType, Func<Type, bool, Control>>()
        {
            {ValueLineType.Enum, (t,b)=>new ComboBox(){ ItemsSource = GetEnums(t,b), VerticalContentAlignment= VerticalAlignment.Center}},
            {ValueLineType.Boolean,(t,b)=>new CheckBox(){ VerticalAlignment= VerticalAlignment.Center, HorizontalAlignment= HorizontalAlignment.Left}},
            {ValueLineType.Number, (t,b)=>new NumericTextBox(){ XIncrement= 10, YIncrement = 1, NullableDecimalConverter = NullableDecimalConverter.Integer}},
            {ValueLineType.DecimalNumber, (t,b)=>new NumericTextBox() },
            {ValueLineType.Currency, (t,b)=>new NumericTextBox(){ XIncrement= 1000, YIncrement = 1, NullableDecimalConverter = NullableDecimalConverter.Currency}},
            {ValueLineType.HighPrecisionNumber, (t,b)=>new NumericTextBox(){ XIncrement= 1, YIncrement = 0.01m, NullableDecimalConverter = NullableDecimalConverter.HighPrecisionNumber}},
            {ValueLineType.String, (t,b)=> new TextBox()},
            {ValueLineType.DateTime,(t,b)=> new DateTimePicker()},
            {ValueLineType.Date,(t,b)=> new DateTimePicker(){ DateTimeConverter= DateTimeConverter.DateOnly }},
            {ValueLineType.Color, (t, b) => new ColorPicker()}
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
        DecimalNumber,
        Currency,
        HighPrecisionNumber, 
        String,
        DateTime,
        Date,
        Color
    };
}
