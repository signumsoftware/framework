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

namespace Signum.Windows
{
    public static class BindExtensions
    {
        public static T Bind<T>(this T frameworkElement, DependencyProperty property, BindingBase binding) where T : FrameworkElement
        {
            frameworkElement.SetBinding(property, binding);
            return frameworkElement;
        }

        public static T Bind<T>(this T frameworkElement, DependencyProperty property, string sourcePath) where T : FrameworkElement
        {
            frameworkElement.SetBinding(property, new Binding(sourcePath));
            return frameworkElement;
        }

        public static T Bind<T, S>(this T frameworkElement, DependencyProperty property, Expression<Func<S, object>> sourcePath) where T : FrameworkElement
        {
            frameworkElement.SetBinding(property, new Binding(RouteVisitor.GetRoute(sourcePath)));
            return frameworkElement;
        }

        public static T Bind<T>(this T frameworkElement, DependencyProperty property, object source, string sourcePath) where T : FrameworkElement
        {
            frameworkElement.SetBinding(property, new Binding(sourcePath) { Source = source });
            return frameworkElement;
        }

        public static T Bind<T, S>(this T frameworkElement, DependencyProperty property, S source, Expression<Func<S, object>> sourcePath) where T : FrameworkElement
        {
            frameworkElement.SetBinding(property, new Binding(RouteVisitor.GetRoute(sourcePath)) { Source = source });
            return frameworkElement;
        }

        public static T Bind<T>(this T frameworkElement, DependencyProperty property, string sourcePath, IValueConverter converter) where T : FrameworkElement
        {
            frameworkElement.SetBinding(property, new Binding(sourcePath) { Converter = converter });
            return frameworkElement;
        }

        public static T Bind<T, S>(this T frameworkElement, DependencyProperty property, Expression<Func<S, object>> sourcePath, IValueConverter converter) where T : FrameworkElement
        {
            frameworkElement.SetBinding(property, new Binding(RouteVisitor.GetRoute(sourcePath)) { Converter = converter });
            return frameworkElement;
        }

        public static T Bind<T>(this T frameworkElement, DependencyProperty property, object source, string sourcePath, IValueConverter converter) where T : FrameworkElement
        {
            frameworkElement.SetBinding(property, new Binding(sourcePath) { Source = source, Converter = converter });
            return frameworkElement;
        }

        public static T Bind<T, S>(this T frameworkElement, DependencyProperty property, S source, Expression<Func<S, object>> sourcePath, IValueConverter converter) where T : FrameworkElement
        {
            frameworkElement.SetBinding(property, new Binding(RouteVisitor.GetRoute(sourcePath)) { Source = source, Converter = converter });
            return frameworkElement;
        }
    }

    public class RouteVisitor : ExpressionVisitor
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
