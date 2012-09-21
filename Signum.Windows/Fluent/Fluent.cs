using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Signum.Utilities.Reflection;
using System.Reflection;
using System.Linq.Expressions;
using System.Windows.Data;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;
using System.ComponentModel;

namespace Signum.Windows
{
    public static class Fluent
    {
        public static T Bind<T>(this T bindable, DependencyProperty property, BindingBase binding) where T : DependencyObject
        {
            BindingOperations.SetBinding(bindable, property, binding);
            return bindable;
        }

        public static T Bind<T>(this T bindable, DependencyProperty property, string sourcePath) where T : DependencyObject
        {
            BindingOperations.SetBinding(bindable, property, new Binding(sourcePath));
            return bindable;
        }

        public static T Bind<T, S>(this T bindable, DependencyProperty property, Expression<Func<S, object>> sourcePath) where T : DependencyObject
        {
            BindingOperations.SetBinding(bindable, property, new Binding(RouteVisitor.GetRoute(sourcePath)));
            return bindable;
        }

        public static T Bind<T>(this T bindable, DependencyProperty property, object source, string sourcePath) where T : DependencyObject
        {
            BindingOperations.SetBinding(bindable, property, new Binding(sourcePath) { Source = source });
            return bindable;
        }

        public static T Bind<T, S>(this T bindable, DependencyProperty property, S source, Expression<Func<S, object>> sourcePath) where T : DependencyObject
        {
            BindingOperations.SetBinding(bindable, property, new Binding(RouteVisitor.GetRoute(sourcePath)) { Source = source });
            return bindable;
        }

        public static T Bind<T>(this T bindable, DependencyProperty property, string sourcePath, IValueConverter converter) where T : DependencyObject
        {
            BindingOperations.SetBinding(bindable, property, new Binding(sourcePath) { Converter = converter });
            return bindable;
        }

        public static T Bind<T, S>(this T bindable, DependencyProperty property, Expression<Func<S, object>> sourcePath, IValueConverter converter) where T : DependencyObject
        {
            BindingOperations.SetBinding(bindable, property, new Binding(RouteVisitor.GetRoute(sourcePath)) { Converter = converter });
            return bindable;
        }

        public static T Bind<T>(this T bindable, DependencyProperty property, object source, string sourcePath, IValueConverter converter) where T : DependencyObject
        {
            BindingOperations.SetBinding(bindable, property, new Binding(sourcePath) { Source = source, Converter = converter });
            return bindable;
        }

        public static T Bind<T, S>(this T bindable, DependencyProperty property, S source, Expression<Func<S, object>> sourcePath, IValueConverter converter) where T : DependencyObject
        {
            BindingOperations.SetBinding(bindable, property, new Binding(RouteVisitor.GetRoute(sourcePath)) { Source = source, Converter = converter });
            return bindable;
        }

        public static T Set<T>(this T depObj, DependencyProperty prop, object value) where T : DependencyObject
        {
            depObj.SetValue(prop, value);
            return depObj;
        }

        public static T Handle<T>(this T uiElement, RoutedEvent routedEvent, RoutedEventHandler handler) where T : UIElement
        {
            uiElement.AddHandler(routedEvent, handler);
            return uiElement;
        }

        public static T ResourceReference<T>(this T frameworkElement, DependencyProperty prop, object value) where T : FrameworkElement
        {
            frameworkElement.SetResourceReference(prop, value);
            return frameworkElement;
        }
     
        public static T Hide<T>(this T uiElement) where T : UIElement
        {
            uiElement.Visibility = Visibility.Hidden;
            return uiElement;
        }

        public static T Collapse<T>(this T uiElement) where T : UIElement
        {
            uiElement.Visibility = Visibility.Collapsed;
            return uiElement;
        }

        public static T Visible<T>(this T uiElement) where T : UIElement
        {
            uiElement.Visibility = Visibility.Visible;
            return uiElement;
        }

        public static T ReadOnly<T>(this T uiElement) where T : UIElement
        {
            Common.SetIsReadOnly(uiElement, true);
            return uiElement;
        }

        public static T Editable<T>(this T uiElement) where T : UIElement
        {
            Common.SetIsReadOnly(uiElement, false);
            return uiElement;
        }

        public static DataTemplate GetDataTemplate(System.Linq.Expressions.Expression<Func<FrameworkElement>> constructor)
        {
            return new DataTemplate
            {
                VisualTree = FrameworkElementFactoryGenerator.Generate(constructor)
            };
        }

        public static FrameworkElementFactory GetFrameworkElementFactory(System.Linq.Expressions.Expression<Func<FrameworkElement>> constructor)
        {
            return FrameworkElementFactoryGenerator.Generate(constructor);
        }

        public static void OnDataContextPropertyChanged(this FrameworkElement fe, PropertyChangedEventHandler propertyChanged)
        {
            fe.DataContextChanged += (object sender, DependencyPropertyChangedEventArgs e) =>
            {
                INotifyPropertyChanged oldDC = e.OldValue as INotifyPropertyChanged;
                if (oldDC != null)
                    oldDC.PropertyChanged -= propertyChanged;

                INotifyPropertyChanged newDC = e.OldValue as INotifyPropertyChanged;
                if (newDC != null)
                    newDC.PropertyChanged += propertyChanged;
            }; 
        }

        public static void OnEntityPropertyChanged(this EntityBase eb, PropertyChangedEventHandler propertyChanged)
        {
            eb.EntityChanged += (object sender, bool userInteraction, object oldValue, object newValue) =>
            {
                INotifyPropertyChanged oldDC = oldValue as INotifyPropertyChanged;
                if (oldDC != null)
                    oldDC.PropertyChanged -= propertyChanged;

                INotifyPropertyChanged newDC = newValue as INotifyPropertyChanged;
                if (newDC != null)
                    newDC.PropertyChanged += propertyChanged;
            };
        }
    }

    class RouteVisitor : SimpleExpressionVisitor
    {
        StringBuilder sb = new StringBuilder();

        public static string GetRoute(LambdaExpression expression)
        {
            RouteVisitor v = new RouteVisitor();
            v.Visit(expression.Body);
            return v.sb.ToString();
        }

        protected override System.Linq.Expressions.Expression VisitMemberAccess(MemberExpression m)
        {
            var result = base.VisitMemberAccess(m);
            if (sb.Length != 0)
                sb.Append(".");
            sb.Append(m.Member.Name);
            return result;
        }

        protected override System.Linq.Expressions.Expression VisitMethodCall(MethodCallExpression m)
        {
            var result = base.VisitMethodCall(m);
            if (m.Method.DeclaringType == typeof(MListExtensions) && m.Method.Name == "Element")
            {
                sb.Append("/");
            }

            return result;
        }

        protected override System.Linq.Expressions.Expression VisitBinary(BinaryExpression b)
        {
            var result = base.VisitBinary(b);

            if (result.NodeType == ExpressionType.ArrayIndex)
            {
                int value = (int)ExpressionEvaluator.Eval(b.Left);
                sb.Append("[" + value + "]");
            }

            return result;
        }
    }
}
