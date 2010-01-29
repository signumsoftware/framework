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

namespace Signum.Windows
{
    public static class Common
    {
        public static readonly DependencyProperty LabelWidthProperty =
           DependencyProperty.RegisterAttached("LabelWidth", typeof(double), typeof(Common), new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.Inherits));
        public static double GetLabelWidth(DependencyObject obj)
        {
            return (double)obj.GetValue(LabelWidthProperty);
        }
        public static void SetLabelWidth(DependencyObject obj, double value)
        {
            obj.SetValue(LabelWidthProperty, value);
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
                   DependencyProperty.RegisterAttached("CollapseIfNull", typeof(bool), typeof(Common), new UIPropertyMetadata(false, (d, e) => CollapseIfNullChanged((FrameworkElement)d)));
        public static bool GetCollapseIfNull(DependencyObject obj)
        {
            return (bool)obj.GetValue(CollapseIfNullProperty);
        }

        public static void SetCollapseIfNull(DependencyObject obj, bool value)
        {
            obj.SetValue(CollapseIfNullProperty, value);
        }

        static object CollapseIfNullChanged(FrameworkElement frameworkElement)
        {
            if (GetRoute(frameworkElement) != null)
                throw new InvalidOperationException(Resources.CollapseIfNullHasToBeSetBeforeRoute);
            return null;
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

        static readonly Regex validIdentifier = new Regex(@"^[_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}][_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}\p{Nd}]*$");
        public static void RoutePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement fe = (FrameworkElement)d;
            if (DesignerProperties.GetIsInDesignMode(fe))
            {
                DependencyProperty labelText =
                    fe is ValueLine ? ValueLine.LabelTextProperty :
                    fe is EntityBase ? EntityBase.LabelTextProperty :
                    null;

                if (labelText != null && fe.NotSet(labelText))
                {
                    fe.SetValue(labelText, e.NewValue);
                }
                return;
            }

            string route = (string)e.NewValue;

            if (!delayRoutes)
                InititializeRoute(fe, route);
            else
            {
                if (fe is IPreLoad)
                    ((IPreLoad)fe).PreLoad += (s, e2) => InititializeRoute(fe, route);
                else
                    fe.Loaded += (s, e2) => InititializeRoute(fe, route);
            }
        }

        private static void InititializeRoute(FrameworkElement fe, string route)
        {
            PropertyRoute context = GetTypeContext(fe.Parent);

            if (context == null)
                throw new ApplicationException(Properties.Resources.RoutePropertyCanNotBeAppliedWithNullTypeContext + ": '{0}'".Formato(route));

            bool pseudoRoute = route.StartsWith("$");
            if (pseudoRoute)
                route = route.Substring(1);

            string[] steps = route.Replace("/", ".Item.").Split('.').Where(s => s.Length > 0).ToArray();

            foreach (var step in steps)
            {
                if (!validIdentifier.IsMatch(step))
                    throw new ApplicationException(Resources.IsNotAValidIdentifier.Formato(step));

                context = context.Add(step);
            }

            if (!pseudoRoute)
            {
                SetTypeContext(fe, context);

                foreach (CommonRouteTask task in RouteTask.GetInvocationList())
                    task(fe, route, context);
            }
            else
            {
                foreach (CommonRouteTask task in PseudoRouteTask.GetInvocationList())
                    task(fe, route, context);
            }
        }

        #region Tasks
        public static event CommonRouteTask RouteTask;

        public static event CommonRouteTask PseudoRouteTask;

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

            PseudoRouteTask += TaskSetLabelText;
        }

        public static void TaskSetValueProperty(FrameworkElement fe, string route, PropertyRoute context)
        {
            DependencyProperty valueProperty =
                fe is LineBase ? ((LineBase)fe).CommonRouteValue() :
                FrameworkElement.DataContextProperty;


            bool isReadOnly = context.PropertyRouteType == PropertyRouteType.Property && context.PropertyInfo.IsReadOnly();

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
               null;

            if (labelText != null && fe.NotSet(labelText))
            {
                fe.SetValue(labelText, (context as PropertyRoute).TryCC(ts => ts.PropertyInfo.NiceName()));
            }
        }

        static void TaskSetUnitText(FrameworkElement fe, string route, PropertyRoute context)
        {
            ValueLine vl = fe as ValueLine;
            if (vl != null && vl.NotSet(ValueLine.UnitTextProperty) && context.PropertyRouteType == PropertyRouteType.Property)
            {
                UnitAttribute ua = context.PropertyInfo.SingleAttribute<UnitAttribute>();
                if (ua != null)
                    vl.UnitText = ua.UnitName;
            }
        }

        static void TaskSetFormatText(FrameworkElement fe, string route, PropertyRoute context)
        {
            ValueLine vl = fe as ValueLine;
            if (vl != null && vl.NotSet(ValueLine.FormatProperty) && context.PropertyRouteType == PropertyRouteType.Property)
            {
                string format = Reflector.FormatString(context.PropertyInfo);
                if (format != null)
                    vl.Format = format;
            }
        }

        public static void TaskSetIsReadonly(FrameworkElement fe, string route, PropertyRoute context)
        {
            bool isReadOnly = context.PropertyRouteType == PropertyRouteType.Property && context.PropertyInfo.IsReadOnly();

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

                    if (Navigator.Manager.ServerTypes.ContainsKey(entityContext.IdentifiableType))
                        eb.Implementations = Server.Return((IBaseServer s) => s.FindImplementations(context));
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
                    Converter = Converters.NullToVisibility
                };
                fe.SetBinding(FrameworkElement.VisibilityProperty, b);
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

        public static readonly DependencyProperty CurrentWindowProperty =
            DependencyProperty.RegisterAttached("CurrentWindow", typeof(Window), typeof(Common), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        public static Window GetCurrentWindow(DependencyObject obj)
        {
            return (Window)obj.GetValue(CurrentWindowProperty);
        }
        public static void SetCurrentWindow(DependencyObject obj, Window value)
        {
            obj.SetValue(CurrentWindowProperty, value);
        }

        public static Window FindCurrentWindow(this FrameworkElement fe)
        {
            return fe.FollowC(a => (FrameworkElement)(a.Parent ?? a.TemplatedParent))
                      .Select(a => Common.GetCurrentWindow(a) ?? a as Window).NotNull().First(Properties.Resources.ParentWindowNotFound);
        }
    }

    public delegate void ChangeDataContextHandler(object sender, ChangeDataContextEventArgs e);

    public class ChangeDataContextEventArgs : RoutedEventArgs
    {
        public object NewDataContext { get; set; }

        public ChangeDataContextEventArgs(object newDataContext)
            : base(Common.ChangeDataContextEvent)
        {
            this.NewDataContext = newDataContext;
        }
    }

    public delegate void CommonRouteTask(FrameworkElement fe, string route, PropertyRoute context);
}
