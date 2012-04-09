using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System.Windows.Data;
using Signum.Utilities.DataStructures;
using System.Reflection;
using System.Windows.Media;
using Signum.Entities.Reflection;
using Signum.Windows.Properties;
using System.Collections;
using Signum.Services;
using Signum.Entities;
using Signum.Utilities.Reflection;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace Signum.Windows
{
    public static class Common
    {
        public static readonly DependencyProperty MinLabelWidthProperty =
           DependencyProperty.RegisterAttached("MinLabelWidth", typeof(double), typeof(Common), new FrameworkPropertyMetadata(120.0, FrameworkPropertyMetadataOptions.Inherits));
        public static double GetMinLabelWidth(DependencyObject obj)
        {
            return (double)obj.GetValue(MinLabelWidthProperty);
        }
        public static void SetMinLabelWidth(DependencyObject obj, double value)
        {
            obj.SetValue(MinLabelWidthProperty, value);
        }


        public static readonly DependencyProperty LabelVisibleProperty =
            DependencyProperty.RegisterAttached("LabelVisible", typeof(bool), typeof(Common), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));
        public static bool GetLabelVisible(DependencyObject obj)
        {
            return (bool)obj.GetValue(LabelVisibleProperty);
        }
        public static void SetLabelVisible(DependencyObject obj, bool value)
        {
            obj.SetValue(LabelVisibleProperty, value);
        }


        public static readonly DependencyProperty IsReadOnlyProperty =
          DependencyProperty.RegisterAttached("IsReadOnly", typeof(bool), typeof(Common), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
        public static bool GetIsReadOnly(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsReadOnlyProperty);
        }
        public static void SetIsReadOnly(DependencyObject obj, bool value)
        {
            obj.SetValue(IsReadOnlyProperty, value);
        }

        [TypeConverter(typeof(PropertyRouteConverter))]
        public static readonly DependencyProperty TypeContextProperty =
            DependencyProperty.RegisterAttached("TypeContext", typeof(PropertyRoute), typeof(Common), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        [TypeConverter(typeof(PropertyRouteConverter))]
        public static PropertyRoute GetTypeContext(DependencyObject obj)
        {
            return (PropertyRoute)obj.GetValue(TypeContextProperty);
        }
        [TypeConverter(typeof(PropertyRouteConverter))]
        public static void SetTypeContext(DependencyObject obj, PropertyRoute value)
        {
            obj.SetValue(TypeContextProperty, value);
        }

        public static readonly DependencyProperty CollapseIfNullProperty =
                   DependencyProperty.RegisterAttached("CollapseIfNull", typeof(bool), typeof(Common), new UIPropertyMetadata(false));
        public static bool GetCollapseIfNull(DependencyObject obj)
        {
            return (bool)obj.GetValue(CollapseIfNullProperty);
        }

        public static void SetCollapseIfNull(DependencyObject obj, bool value)
        {
            obj.SetValue(CollapseIfNullProperty, value);
        }

        public static readonly DependencyProperty RouteProperty =
         DependencyProperty.RegisterAttached("Route", typeof(string), typeof(Common), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(RoutePropertyChanged)));
        public static string GetRoute(this DependencyObject obj)
        {
            return (string)obj.GetValue(RouteProperty);
        }
        public static void SetRoute(this DependencyObject obj, string value)
        {
            obj.SetValue(RouteProperty, value);
        }

        public static readonly DependencyProperty LabelOnlyRouteProperty =
         DependencyProperty.RegisterAttached("LabelOnlyRoute", typeof(string), typeof(Common), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(RoutePropertyChanged)));
        public static string GetLabelOnlyRoute(this DependencyObject obj)
        {
            return (string)obj.GetValue(LabelOnlyRouteProperty);
        }
        public static void SetLabelOnlyRoute(this DependencyObject obj, string value)
        {
            obj.SetValue(LabelOnlyRouteProperty, value);
        }

        [ThreadStatic]
        static bool delayRoutes = false;

        public static IDisposable DelayRoutes()
        {
            if (delayRoutes)
                return null;

            delayRoutes = true;

            return new Disposable(() =>
            {
                delayRoutes = false;
            });
        }


        public static readonly DependencyProperty DelayedRoutesProperty =
            DependencyProperty.RegisterAttached("DelayedRoutes", typeof(bool), typeof(Common), new UIPropertyMetadata(false, DelayedRoutesChanged));
        public static bool GetDelayedRoutes(DependencyObject obj)
        {
            return (bool)obj.GetValue(DelayedRoutesProperty);
        }
        public static void SetDelayedRoutes(DependencyObject obj, bool value)
        {
            obj.SetValue(DelayedRoutesProperty, value);
        }

        public static void DelayedRoutesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement fe = (FrameworkElement)d;

            IDisposable del = DelayRoutes();

            if (del != null)
                fe.Initialized += (s, e2) => del.Dispose();
        }

        public static void RoutePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement fe = d as FrameworkElement;
            if (fe == null)
                return;

            if (DesignerProperties.GetIsInDesignMode(fe))
            {
                DependencyProperty labelText =
                    fe is LineBase ? ((LineBase)fe).CommonRouteLabelText() :
                    fe is HeaderedContentControl ? HeaderedContentControl.HeaderProperty :
                    fe is TextBlock ? TextBlock.TextProperty :
                    fe is Label ? Label.ContentProperty :
                    null;

                if (labelText != null && fe.NotSet(labelText))
                {
                    fe.SetValue(labelText, e.NewValue);
                }
                return;
            }

            string route = (string)e.NewValue;

            if (!delayRoutes)
                InititializeRoute(fe, route, e.Property);
            else
            {
                if (fe is IPreLoad)
                    ((IPreLoad)fe).PreLoad += (s, e2) => InititializeRoute(fe, route, e.Property);
                else
                    fe.Loaded += (s, e2) => InititializeRoute(fe, route, e.Property);
            }

            if(fe is DataGrid)
                fe.Initialized += new EventHandler(fe_Initialized);
        }

        static void fe_Initialized(object sender, EventArgs e)
        {
            DataGrid grid = (DataGrid)sender;
            PropertyRoute parentContext = GetTypeContext(grid).Add("Item");

            SetTypeContext(grid, parentContext); 

            foreach (DataGridColumn column in grid.Columns)
            {
                if (column.IsSet(Common.RouteProperty))
                {
                    string route = (string)column.GetValue(Common.RouteProperty);
                    PropertyRoute context = ContinueRouteExtension.Continue(parentContext, route);

                    SetTypeContext(column, context);

                    foreach (ColumnCommonRouteTask task in ColumnRouteTask.GetInvocationList())
                        task(column, route, context);
                }
                else
                {
                    if (column.IsSet(Common.LabelOnlyRouteProperty))
                    {
                        string route = (string)column.GetValue(Common.LabelOnlyRouteProperty);
                        PropertyRoute context = ContinueRouteExtension.Continue(parentContext, route);

                        foreach (ColumnCommonRouteTask task in ColumnLabelOnlyRouteTask.GetInvocationList())
                            task(column, route, context);
                    }
                }
            }
        }

        public enum RouteType
        {
            All, 
            TypeContextOnly, 
            LabelOnly, 
        }

        private static void InititializeRoute(FrameworkElement fe, string route, DependencyProperty property)
        {
            PropertyRoute parentContext = GetTypeContext(fe.Parent ?? fe);

            if (parentContext == null)
                throw new InvalidOperationException("Route attached property can not be set with null TypeContext: '{0}'".Formato(route));

            var context = ContinueRouteExtension.Continue(parentContext, route); 

            if (property == Common.RouteProperty)
            {
                SetTypeContext(fe, context);

                foreach (CommonRouteTask task in RouteTask.GetInvocationList())
                    task(fe, route, context);
            }
            else
            {
                foreach (CommonRouteTask task in LabelOnlyRouteTask.GetInvocationList())
                    task(fe, route, context);
            }
        }

        #region Tasks
        public static event CommonRouteTask RouteTask;
        public static event CommonRouteTask LabelOnlyRouteTask;

        public static event ColumnCommonRouteTask ColumnRouteTask;
        public static event ColumnCommonRouteTask ColumnLabelOnlyRouteTask;

        static Common()
        {
            RouteTask += TaskSetTypeProperty;
            RouteTask += TaskSetValueProperty;
            RouteTask += TaskSetLabelText;
            RouteTask += TaskSetUnitText;
            RouteTask += TaskSetFormatText;
            RouteTask += TaskSetIsReadonly;
            RouteTask += TaskSetImplementations;
            RouteTask += TaskSetCollaspeIfNull;
            RouteTask += TaskSetNotNullItemsSource;

            LabelOnlyRouteTask += TaskSetLabelText;

            ColumnRouteTask += TaskColumnSetValueProperty;
            ColumnRouteTask += TaskColumnSetLabelText;
            ColumnRouteTask += TaskColumnSetReadOnly;
            ColumnLabelOnlyRouteTask += TaskColumnSetLabelText;
        }

        static void TaskColumnSetReadOnly(DataGridColumn column, string route, PropertyRoute context)
        {
            bool isReadOnly = context.PropertyRouteType == PropertyRouteType.FieldOrProperty && context.PropertyInfo.IsReadOnly();

            if (isReadOnly && column.NotSet(DataGridColumn.IsReadOnlyProperty))
            {
                column.IsReadOnly = isReadOnly;
            }
        }

        public static void TaskColumnSetValueProperty(DataGridColumn col, string route, PropertyRoute context)
        {
            DataGridBoundColumn colBound = col as DataGridBoundColumn;

            if (col == null)
                return;

            if (colBound.Binding == null)
            {
                bool isReadOnly = context.PropertyRouteType == PropertyRouteType.FieldOrProperty && context.PropertyInfo.IsReadOnly();
                colBound.Binding = new Binding(route)
                {
                    Mode = isReadOnly ? BindingMode.OneWay : BindingMode.TwoWay,
                    NotifyOnValidationError = true,
                    ValidatesOnExceptions = true,
                    ValidatesOnDataErrors = true,
                    Converter = GetConverter(context),
                };
            }
        }

        private static IValueConverter GetConverter(PropertyRoute context)
        {
            string format = Reflector.FormatString(context);
            var valueType = ValueLine.Configurator.GetDefaultValueLineType(context.Type);
            if (valueType == ValueLineType.Number)
                return format == NullableNumericConverter.Integer.Format ? NullableNumericConverter.Integer :
                       format == NullableNumericConverter.Number.Format ? NullableNumericConverter.Number :
                       new NullableNumericConverter(format);

            if (valueType == ValueLineType.DateTime)
                return format == DateTimeConverter.DateAndTime.Format ? DateTimeConverter.DateAndTime :
                       format == DateTimeConverter.Date.Format ? DateTimeConverter.Date :
                       new DateTimeConverter(format);

            return null;
        }

        public static void TaskColumnSetLabelText(DataGridColumn col, string route, PropertyRoute context)
        {
            DependencyProperty labelText = DataGridColumn.HeaderProperty;

            if (labelText != null && col.NotSet(labelText))
            {
                string text = context.PropertyInfo.NiceName();

                UnitAttribute ua = context.PropertyInfo.SingleAttribute<UnitAttribute>();
                if (ua != null)
                    text += " (" + ua.UnitName + ")";

                col.SetValue(labelText, text);
            }
        }

        public static void TaskSetValueProperty(FrameworkElement fe, string route, PropertyRoute context)
        {
            DependencyProperty valueProperty = ValueProperty(fe);

            bool isReadOnly = context.PropertyRouteType == PropertyRouteType.FieldOrProperty && context.PropertyInfo.IsReadOnly();

            if (!BindingOperations.IsDataBound(fe, valueProperty))
            {
                Binding b = new Binding(route)
                {
                    Mode = isReadOnly ? BindingMode.OneWay : BindingMode.TwoWay,
                    NotifyOnValidationError = true,
                    ValidatesOnExceptions = true,
                    ValidatesOnDataErrors = true,
                };
                fe.SetBinding(valueProperty, b);
            }
        }

        private static DependencyProperty ValueProperty(FrameworkElement fe)
        {
            return fe is LineBase ? ((LineBase)fe).CommonRouteValue() :
                            fe is DataGrid ? DataGrid.ItemsSourceProperty :
                            FrameworkElement.DataContextProperty;
        }


        public static void TaskSetTypeProperty(FrameworkElement fe, string route, PropertyRoute context)
        {
            DependencyProperty typeProperty =
                fe is LineBase ? ((LineBase)fe).CommonRouteType() :
                null;

            if (typeProperty != null && fe.NotSet(typeProperty))
            {
                fe.SetValue(typeProperty, context.Type);
            }
        }

        public static void TaskSetLabelText(FrameworkElement fe, string route, PropertyRoute context)
        {
            DependencyProperty labelText =
               fe is LineBase ? ((LineBase)fe).CommonRouteLabelText() :
               fe is HeaderedContentControl ? HeaderedContentControl.HeaderProperty :
               fe is TextBlock ? TextBlock.TextProperty:
               fe is Label ? Label.ContentProperty :
               null;

            if (labelText != null && fe.NotSet(labelText))
            {
                fe.SetValue(labelText, context.PropertyInfo.NiceName());
            }
        }

        static void TaskSetUnitText(FrameworkElement fe, string route, PropertyRoute context)
        {
            ValueLine vl = fe as ValueLine;
            if (vl != null && vl.NotSet(ValueLine.UnitTextProperty) && context.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {
                UnitAttribute ua = context.PropertyInfo.SingleAttribute<UnitAttribute>();
                if (ua != null)
                    vl.UnitText = ua.UnitName;
            }
        }

        static void TaskSetFormatText(FrameworkElement fe, string route, PropertyRoute context)
        {
            ValueLine vl = fe as ValueLine;
            if (vl != null && vl.NotSet(ValueLine.FormatProperty) && context.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {
                string format = Reflector.FormatString(context);
                if (format != null)
                    vl.Format = format;
            }
        }

        public static void TaskSetIsReadonly(FrameworkElement fe, string route, PropertyRoute context)
        {
            bool isReadOnly = context.PropertyRouteType == PropertyRouteType.FieldOrProperty && context.PropertyInfo.IsReadOnly();

            if (isReadOnly && fe.NotSet(Common.IsReadOnlyProperty) && (fe is ValueLine || fe is EntityLine || fe is EntityCombo || fe is TextArea))
            {
                Common.SetIsReadOnly(fe, isReadOnly);
            }
        }

        public static void TaskSetImplementations(FrameworkElement fe, string route, PropertyRoute context)
        {
            EntityBase eb = fe as EntityBase;
            if (eb != null && eb.NotSet(EntityBase.ImplementationsProperty))
            {
                PropertyRoute entityContext = eb.GetEntityTypeContext();

                if (entityContext != null)
                {
                    eb.Implementations = entityContext.GetImplementations();
                }
            }
        }

        static void TaskSetCollaspeIfNull(FrameworkElement fe, string route, PropertyRoute context)
        {
            if (GetCollapseIfNull(fe) && fe.NotSet(UIElement.VisibilityProperty))
            {
                var dp = ValueProperty(fe);

                if (dp == null)
                    throw new InvalidOperationException("Unknown value property of {0}".Formato(fe.GetType().Name));

                Binding b = new Binding()
                {
                    Path = new PropertyPath(dp.Name),
                    Source = fe,
                    Mode = BindingMode.OneWay,
                    Converter = Converters.NullToVisibility,
                };
                fe.SetBinding(FrameworkElement.VisibilityProperty, b);
            }
        }
        
        static void TaskSetNotNullItemsSource(FrameworkElement fe, string route, PropertyRoute context)
        {
            ValueLine vl = fe as ValueLine;
            if (vl != null && vl.NotSet(ValueLine.ItemSourceProperty) && context.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {
                if(context.Type.IsNullable() && context.Type.UnNullify().IsEnum &&
                   Validator.GetOrCreatePropertyPack(context).Validators.OfType<NotNullableAttribute>().Any())
                {
                    vl.ItemSource = EnumExtensions.UntypedGetValues(vl.Type.UnNullify());
                }
            }
        }

        #endregion

        public static readonly RoutedEvent ChangeDataContextEvent = EventManager.RegisterRoutedEvent("ChangeDataContext", RoutingStrategy.Bubble, typeof(ChangeDataContextHandler), typeof(Common));
        public static void AddChangeDataContextHandler(DependencyObject d, ChangeDataContextHandler handler)
        {
            ((UIElement)d).AddHandler(ChangeDataContextEvent, handler);
        }
        public static void RemoveChangeDataContextHandler(DependencyObject d, ChangeDataContextHandler handler)
        {
            ((UIElement)d).RemoveHandler(ChangeDataContextEvent, handler);
        }

        public static IDisposable OverrideCursor(System.Windows.Input.Cursor cursor)
        {
            Mouse.OverrideCursor = cursor;
            return new Disposable(() => Mouse.OverrideCursor = null);
        }
    }

    public delegate void ChangeDataContextHandler(object sender, ChangeDataContextEventArgs e);

    public class ChangeDataContextEventArgs : RoutedEventArgs
    {
        public object NewDataContext { get; set; }
        public bool Refresh { get; set; }

        public ChangeDataContextEventArgs(object newDataContext)
            : base(Common.ChangeDataContextEvent)
        {
            this.NewDataContext = newDataContext;
        }

        public ChangeDataContextEventArgs()
            : base(Common.ChangeDataContextEvent)
        {
            this.Refresh = true;
        }
    }

    public delegate void CommonRouteTask(FrameworkElement fe, string route, PropertyRoute context);
    public delegate void ColumnCommonRouteTask(DataGridColumn column, string route, PropertyRoute context);
}
