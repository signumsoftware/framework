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
using System.Collections;
using Signum.Services;
using Signum.Entities;
using Signum.Utilities.Reflection;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Automation;
using System.Diagnostics;

namespace Signum.Windows
{
    public enum AutoHide
    {
        Undefined, 
        Collapsed,
        Visible, 
    }

    public static class Common
    {
        public static readonly DependencyProperty AutoHideProperty =
           DependencyProperty.RegisterAttached("AutoHide", typeof(AutoHide), typeof(Common), new UIPropertyMetadata(AutoHide.Undefined, AutoHidePropertyChanged));
        public static AutoHide GetAutoHide(DependencyObject obj)
        {
            return (AutoHide)obj.GetValue(AutoHideProperty);
        }

        public static void SetAutoHide(DependencyObject obj, AutoHide value)
        {
            obj.SetValue(AutoHideProperty, value);
        }

        public static void AutoHidePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            if (d is FrameworkElement fe && e.NewValue is AutoHide)
                fe.Loaded += new RoutedEventHandler(Common_Loaded);
        }

        static void Common_Loaded(object sender, RoutedEventArgs e)
        {
            var fe = (FrameworkElement)sender;

            fe.Loaded -= Common_Loaded;

            if (Common.GetAutoHide(fe) == AutoHide.Collapsed)
                fe.Visibility = Visibility.Collapsed;
        }

        public static void VoteVisible(FrameworkElement fe)
        {
            foreach (var item in fe.LogicalParents().TakeWhile(a => GetAutoHide(a) != AutoHide.Visible))
            {
                SetAutoHide(item, AutoHide.Visible);
            }
        }

        public static void VoteCollapsed(FrameworkElement fe)
        {
            foreach (var item in fe.LogicalParents().TakeWhile(a => GetAutoHide(a) == AutoHide.Undefined))
            {
                SetAutoHide(item, AutoHide.Collapsed);
            }
        }

        public static void RefreshAutoHide(FrameworkElement content)
        {
            var list = content.Children<FrameworkElement>(fe => GetAutoHide(fe) != AutoHide.Undefined, WhereFlags.StartOnParent).ToList();

            foreach (var item in list)
            {
                var ah = Common.GetAutoHide(item);

                if (item.Parent is FrameworkElement)
                {
                    if (ah == AutoHide.Visible)
                        Common.VoteVisible((FrameworkElement)item.Parent);
                    else if (ah == AutoHide.Collapsed)
                        Common.VoteCollapsed((FrameworkElement)item.Parent);
                }
            }
        }

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

        public static readonly DependencyProperty OrderProperty =
         DependencyProperty.RegisterAttached("Order", typeof(double), typeof(Common), new PropertyMetadata(0.0));
        public static double GetOrder(DependencyObject obj)
        {
            return (double)obj.GetValue(OrderProperty);
        }

        public static void SetOrder(DependencyObject obj, double value)
        {
            obj.SetValue(OrderProperty, value);
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

        public static readonly DependencyProperty PropertyRouteProperty =
            DependencyProperty.RegisterAttached("PropertyRoute", typeof(PropertyRoute), typeof(Common), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, (s, e) => { }));
        public static PropertyRoute GetPropertyRoute(DependencyObject obj)
        {
            return (PropertyRoute)obj.GetValue(PropertyRouteProperty);
        }
        public static void SetPropertyRoute(DependencyObject obj, PropertyRoute value)
        {
            obj.SetValue(PropertyRouteProperty, value);
        }


        //Angabanga style! http://signum.codeplex.com/discussions/407307
        public static readonly DependencyProperty TypeContextProperty =
            DependencyProperty.RegisterAttached("TypeContext", typeof(Type), typeof(Common), 
            new PropertyMetadata(OnSetTypeContext));             
        public static Type GetTypeContext(DependencyObject obj)
        {
            return (Type)obj.GetValue(TypeContextProperty);
        }

        public static void SetTypeContext(DependencyObject obj, Type value)
        {
            obj.SetValue(TypeContextProperty, value);
        }

        static void OnSetTypeContext(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            Common.SetPropertyRoute(sender, args.NewValue == null ? null : PropertyRoute.Root((Type)args.NewValue));
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

        public static void RoutePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement fe = d as FrameworkElement;
            if (fe == null || e.NewValue == null && e.OldValue != null)
                return;

            if (DesignerProperties.GetIsInDesignMode(fe))
            {
                DependencyProperty labelText = LabelPropertySelector.TryGetValue(fe.GetType());

                if (labelText != null && fe.NotSet(labelText))
                {
                    fe.SetValue(labelText, e.NewValue);
                }
                return;
            }


            string route = (string)e.NewValue;

            var parent = GetPropertyRoute(fe.Parent ?? fe);

            if (parent != null)
                InititializeRoute(fe, route, e.Property);
            else
            {
                if (fe is IPreLoad)
                    ((IPreLoad)fe).PreLoad += (s, e2) => InititializeRoute(fe, route, e.Property);
                else
                    fe.Loaded += (s, e2) => InititializeRoute(fe, route, e.Property);
            }

            if (fe is DataGrid)
                fe.Initialized += DataGrid_Initialized;

            if (fe is ListView)
                fe.Initialized += ListView_Initialized;
        }

        static void ListView_Initialized(object sender, EventArgs e)
        {
            ListView lv = (ListView)sender;
            PropertyRoute parentContext = GetPropertyRoute(lv).Add("Item");

            SetPropertyRoute(lv, parentContext);

            foreach (GridViewColumn column in ((GridView)lv.View).Columns)
            {
                if (column.IsSet(Common.RouteProperty))
                {
                    string route = (string)column.GetValue(Common.RouteProperty);
                    PropertyRoute context = ContinueRouteExtension.Continue(parentContext, route);

                    SetPropertyRoute(column, context);

                    foreach (var task in GridViewColumnRouteTask.GetInvocationListTyped())
                        task(column, route, context);
                }
                else
                {
                    if (column.IsSet(Common.LabelOnlyRouteProperty))
                    {
                        string route = (string)column.GetValue(Common.LabelOnlyRouteProperty);
                        PropertyRoute context = ContinueRouteExtension.Continue(parentContext, route);

                        foreach (var task in GridViewColumnLabelOnlyRouteTask.GetInvocationListTyped())
                            task(column, route, context);
                    }
                }
            }
        }

        static void DataGrid_Initialized(object sender, EventArgs e)
        {
            DataGrid grid = (DataGrid)sender;
            PropertyRoute parentContext = GetPropertyRoute(grid).Add("Item");

            SetPropertyRoute(grid, parentContext); 

            foreach (DataGridColumn column in grid.Columns)
            {
                if (column.IsSet(Common.RouteProperty))
                {
                    string route = (string)column.GetValue(Common.RouteProperty);
                    PropertyRoute context = ContinueRouteExtension.Continue(parentContext, route);

                    SetPropertyRoute(column, context);

                    foreach (var task in DataGridColumnRouteTask.GetInvocationListTyped())
                        task(column, route, context);
                }
                else
                {
                    if (column.IsSet(Common.LabelOnlyRouteProperty))
                    {
                        string route = (string)column.GetValue(Common.LabelOnlyRouteProperty);
                        PropertyRoute context = ContinueRouteExtension.Continue(parentContext, route);

                        foreach (var task in DataGridColumnLabelOnlyRouteTask.GetInvocationListTyped())
                            task(column, route, context);
                    }
                }
            }
        }

        private static void InititializeRoute(FrameworkElement fe, string route, DependencyProperty property)
        {
            PropertyRoute parentContext = GetPropertyRoute(fe.Parent ?? fe);

            if (parentContext == null)
                throw new InvalidOperationException("Route attached property can not be set with null PropertyRoute: '{0}'".FormatWith(route));

            var context = ContinueRouteExtension.Continue(parentContext, route); 

            if (property == Common.RouteProperty)
            {
                SetPropertyRoute(fe, context);

                foreach (var task in RouteTask.GetInvocationListTyped())
                    task(fe, route, context);
            }
            else
            {
                foreach (var task in LabelOnlyRouteTask.GetInvocationListTyped())
                    task(fe, route, context);
            }
        }

        #region Tasks
        public static event CommonRouteTask RouteTask;
        public static event CommonRouteTask LabelOnlyRouteTask;

        public static event DataGridColumnCommonRouteTask DataGridColumnRouteTask;
        public static event DataGridColumnCommonRouteTask DataGridColumnLabelOnlyRouteTask;

        public static event GridViewColumnCommonRouteTask GridViewColumnRouteTask;
        public static event GridViewColumnCommonRouteTask GridViewColumnLabelOnlyRouteTask;


        static Common()
        {
            RouteTask += TaskSetTypeProperty;
            RouteTask += TaskSetValueProperty;
            RouteTask += TaskSetLabelText;
            RouteTask += TaskSetUnitText;
            RouteTask += TaskSetFormatText;
            RouteTask += TaskSetIsReadonly;
            RouteTask += TaskSetImplementations;
            RouteTask += TaskSetMove;
            RouteTask += TaskSetCollaspeIfNull;
            RouteTask += TaskSetNotNullItemsSource;
            RouteTask += TaskSetNullValueEntityCombo;
            RouteTask += TaskSetAutomationName;
            RouteTask += TaskSetVoteAutoHide;
            RouteTask += TaskSetMaxLenth;

            LabelOnlyRouteTask += TaskSetLabelText;
            LabelOnlyRouteTask += TaskSetVoteAutoHide;

            DataGridColumnRouteTask += TaskDataGridColumnSetValueProperty;
            DataGridColumnRouteTask += TaskDataGridColumnSetLabelText;
            DataGridColumnRouteTask += TaskDataGridColumnSetReadOnly;
            DataGridColumnLabelOnlyRouteTask += TaskDataGridColumnSetLabelText;

            GridViewColumnRouteTask += TaskGridViewColumnSetValueProperty;
            GridViewColumnRouteTask += TaskGridViewColumnSetLabelText;
            GridViewColumnLabelOnlyRouteTask += TaskGridViewColumnSetLabelText;

            ValuePropertySelector.SetDefinition(typeof(FrameworkElement), FrameworkElement.DataContextProperty);
            ValuePropertySelector.SetDefinition(typeof(ListView), ItemsControl.ItemsSourceProperty);
            ValuePropertySelector.SetDefinition(typeof(DataGrid), ItemsControl.ItemsSourceProperty);

            TypePropertySelector.SetDefinition(typeof(FrameworkElement), null);

            LabelPropertySelector.SetDefinition(typeof(FrameworkElement), null);
            LabelPropertySelector.SetDefinition(typeof(HeaderedContentControl), HeaderedContentControl.HeaderProperty);
            LabelPropertySelector.SetDefinition(typeof(TextBlock), TextBlock.TextProperty);
            LabelPropertySelector.SetDefinition(typeof(Label), Label.ContentProperty);
        }


        static void TaskSetVoteAutoHide(FrameworkElement fe, string route, PropertyRoute context)
        {
            if (context.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {
                if(context.IsAllowed() == null)
                {
                    VoteVisible(fe);
                }
                else
                {
                    VoteCollapsed(fe);
                }
            }
        }


        
        static void TaskDataGridColumnSetReadOnly(DataGridColumn column, string route, PropertyRoute context)
        {
            bool isReadOnly = context.PropertyRouteType == PropertyRouteType.FieldOrProperty && context.PropertyInfo.IsReadOnly();

            if (isReadOnly && column.NotSet(DataGridColumn.IsReadOnlyProperty))
            {
                column.IsReadOnly = isReadOnly;
            }
        }

        public static void TaskDataGridColumnSetValueProperty(DataGridColumn col, string route, PropertyRoute context)
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

        public static void TaskDataGridColumnSetLabelText(DataGridColumn col, string route, PropertyRoute context)
        {
            DependencyProperty labelText = DataGridColumn.HeaderProperty;

            if (labelText != null && col.NotSet(labelText))
            {
                string text = context.PropertyInfo.NiceName();

                UnitAttribute ua = context.PropertyInfo.GetCustomAttribute<UnitAttribute>();
                if (ua != null)
                    text += " (" + ua.UnitName + ")";

                col.SetValue(labelText, text);
            }
        }



        public static void TaskGridViewColumnSetValueProperty(GridViewColumn col, string route, PropertyRoute context)
        {
            col.DisplayMemberBinding = new Binding(route);
        }

        public static void TaskGridViewColumnSetLabelText(GridViewColumn col, string route, PropertyRoute context)
        {
            DependencyProperty labelText = GridViewColumn.HeaderProperty;

            if (labelText != null && col.NotSet(labelText))
            {
                string text = context.PropertyInfo.NiceName();

                col.SetValue(labelText, text);
            }
        }

        public static Polymorphic<DependencyProperty> ValuePropertySelector = new Polymorphic<DependencyProperty>(minimumType: typeof(FrameworkElement));

        public static void TaskSetValueProperty(FrameworkElement fe, string route, PropertyRoute context)
        {
            DependencyProperty valueProperty = ValuePropertySelector.GetValue(fe.GetType());

            bool isReadOnly = context.PropertyRouteType == PropertyRouteType.FieldOrProperty && context.PropertyInfo.IsReadOnly() || 
                context.PropertyRouteType == PropertyRouteType.Mixin;

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

        public static Polymorphic<DependencyProperty> TypePropertySelector = new Polymorphic<DependencyProperty>(minimumType: typeof(FrameworkElement));

        public static void TaskSetTypeProperty(FrameworkElement fe, string route, PropertyRoute context)
        {
            DependencyProperty typeProperty = TypePropertySelector.TryGetValue(fe.GetType());

            if (typeProperty != null && fe.NotSet(typeProperty))
            {
                fe.SetValue(typeProperty, context.Type);
            }
        }

        public static Polymorphic<DependencyProperty> LabelPropertySelector = new Polymorphic<DependencyProperty>(minimumType: typeof(FrameworkElement));

        public static void TaskSetLabelText(FrameworkElement fe, string route, PropertyRoute context)
        {
            DependencyProperty labelText = LabelPropertySelector.TryGetValue(fe.GetType());

            if (labelText != null && fe.NotSet(labelText))
            {
                fe.SetValue(labelText, context.PropertyInfo.NiceName());
            }
        }

        static void TaskSetUnitText(FrameworkElement fe, string route, PropertyRoute context)
        {
            if (fe is ValueLine vl && vl.NotSet(ValueLine.UnitTextProperty) && context.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {
                UnitAttribute ua = context.PropertyInfo.GetCustomAttribute<UnitAttribute>();
                if (ua != null)
                    vl.UnitText = ua.UnitName;
            }
        }

        static void TaskSetFormatText(FrameworkElement fe, string route, PropertyRoute context)
        {
            if (fe is ValueLine vl && vl.NotSet(ValueLine.FormatProperty) && context.PropertyRouteType == PropertyRouteType.FieldOrProperty)
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
            if (fe is EntityBase eb && eb.NotSet(EntityBase.ImplementationsProperty))
            {
                PropertyRoute entityContext = eb.GetEntityPropertyRoute();

                if (entityContext != null && entityContext.Type.CleanType().IsIEntity())
                {
                    eb.Implementations = entityContext.GetImplementations();
                }
            }
        }

        public static void TaskSetMove(FrameworkElement fe, string route, PropertyRoute context)
        {
            if (fe is EntityListBase eb && eb.NotSet(EntityListBase.MoveProperty))
            {
                if (!eb.Move && context.FieldInfo.HasAttribute<PreserveOrderAttribute>())
                {
                    eb.Move = true;
                }
            }
        }

        static void TaskSetCollaspeIfNull(FrameworkElement fe, string route, PropertyRoute context)
        {
            if (GetCollapseIfNull(fe) && fe.NotSet(UIElement.VisibilityProperty))
            {
                Binding b = new Binding(route)
                {
                    Mode = BindingMode.OneWay,
                    Converter = Converters.NullToVisibility,
                };
                
                fe.SetBinding(FrameworkElement.VisibilityProperty, b);
            }
        }
        
        static void TaskSetNotNullItemsSource(FrameworkElement fe, string route, PropertyRoute context)
        {
            if (fe is ValueLine vl && vl.NotSet(ValueLine.ItemSourceProperty) && context.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {
                if (context.Type.IsNullable() && context.Type.UnNullify().IsEnum &&
                   Validator.TryGetPropertyValidator(context).Let(pv => pv != null && pv.Validators.OfType<NotNullValidatorAttribute>().Any()))
                {
                    vl.ItemSource = EnumExtensions.UntypedGetValues(vl.Type.UnNullify()).ToObservableCollection();
                }
            }
        }

        static void TaskSetNullValueEntityCombo(FrameworkElement fe, string route, PropertyRoute context)
        {
            if (fe is EntityCombo ec && ec.NotSet(EntityCombo.NullValueProperty) && context.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {
                if (Validator.TryGetPropertyValidator(context).Let(pv => pv != null && pv.Validators.OfType<NotNullValidatorAttribute>().Any()))
                {
                    ec.NullValue = false;
                }
            }
        }

        static void TaskSetMaxLenth(FrameworkElement fe, string route, PropertyRoute context)
        {
            if (fe is ValueLine vl && context.PropertyRouteType == PropertyRouteType.FieldOrProperty && context.Type == typeof(string))
            {
                var slv = Validator.TryGetPropertyValidator(context)?.Validators.OfType<StringLengthValidatorAttribute>().FirstOrDefault();
                if (slv != null && slv.Max != -1)
                    vl.MaxTextLength = slv.Max;

                //if (slv != null && slv.MultiLine)
                //    vl.ValueLineType = ValueLineType.TextArea;
            }
        }

        static void TaskSetAutomationName(FrameworkElement fe, string route, PropertyRoute context)
        {
            if (fe.NotSet(AutomationProperties.NameProperty))
            {
                AutomationProperties.SetName(fe, context?.ToString() ?? "");
            }
        }

        public static readonly DependencyProperty AutomationItemStatusFromDataContextProperty =
           DependencyProperty.RegisterAttached("AutomationItemStatusFromDataContext", typeof(bool), typeof(Common), new UIPropertyMetadata(false, RegisterUpdater));
        public static bool GetAutomationItemStatusFromDataContext(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutomationItemStatusFromDataContextProperty);
        }

        public static void SetAutomationItemStatusFromDataContext(DependencyObject obj, bool value)
        {
            obj.SetValue(AutomationItemStatusFromDataContextProperty, value);
        }

        static void RegisterUpdater(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var fe = (FrameworkElement)sender;

            if ((bool)args.NewValue)
            {
                fe.DataContextChanged += Common_DataContextChanged;

                AutomationProperties.SetItemStatus(fe, GetEntityStringAndHashCode(fe.DataContext));
            }
            else
            {
                fe.DataContextChanged -= Common_DataContextChanged;

                AutomationProperties.SetItemStatus(fe, "");
            }
        }

        static void Common_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AutomationProperties.SetItemStatus((DependencyObject)sender, GetEntityStringAndHashCode(e.NewValue));
        }


        public static string GetEntityStringAndHashCode(object newValue)
        {
            if (newValue == null)
                return "";

            return GetEntityString(newValue) + " Hash: " + ReferenceEqualityComparer<object>.Default.GetHashCode(newValue).ToString("x");
        }

        static string GetEntityString(object newValue)
        {
            if (newValue == null)
                return "";

            if (newValue is EmbeddedEntity)
                return newValue.GetType().Name;

            if (newValue is Entity ident)
            {
                if (ident.IsNew)
                    return "{0};New".FormatWith(Server.ServerTypes[ident.GetType()].CleanName);

                return ident.ToLite().Key();
            }

            if (newValue is Lite<IEntity> lite)
            {
                if (lite.EntityOrNull != null && lite.EntityOrNull.IsNew)
                    return "{0};New".FormatWith(Server.ServerTypes[lite.EntityType].CleanName);

                return lite.Key();
            }

            return "";
        }

        #endregion

        public static readonly RoutedEvent ChangeDataContextEvent = EventManager.RegisterRoutedEvent("ChangeDataContext", RoutingStrategy.Bubble, typeof(ChangeDataContextHandler), typeof(Common));
        public static readonly RoutedEvent CloseFormEvent = EventManager.RegisterRoutedEvent("CloseForm", RoutingStrategy.Bubble, typeof(CloseFormHandler), typeof(Common));
        public static void AddChangeDataContextHandler(DependencyObject d, ChangeDataContextHandler handler)
        {
            ((UIElement)d).AddHandler(ChangeDataContextEvent, handler);
        }
        public static void AddCloseFormHandler(DependencyObject d, CloseFormHandler handler)
        {
            ((UIElement)d).AddHandler(CloseFormEvent, handler);
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
    public delegate void CloseFormHandler(object sender, CloseFormEventArgs e);

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

    public class CloseFormEventArgs : RoutedEventArgs
    {
        public CloseFormEventArgs() : base(Common.CloseFormEvent) { }
    }

    public delegate void CommonRouteTask(FrameworkElement fe, string route, PropertyRoute context);
    public delegate void DataGridColumnCommonRouteTask(DataGridColumn column, string route, PropertyRoute context);
    public delegate void GridViewColumnCommonRouteTask(GridViewColumn column, string route, PropertyRoute context);
}
