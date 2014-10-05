using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using w = System.Windows;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Reflection;
using System.Windows.Controls;
using Signum.Utilities.ExpressionTrees;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Markup;

namespace Signum.Windows
{
    static class FrameworkElementFactoryGenerator
    {
        public static w.FrameworkElementFactory Generate(Expression<Func<w.FrameworkElement>> expression)
        {
            ParameterExpression rootFactory = Expression.Parameter(typeof(w.FrameworkElementFactory), "f");
            var list = Process(expression.Body, rootFactory);
            var variables = list.Where(a => a.NodeType == ExpressionType.Assign).Select(e => (ParameterExpression)((BinaryExpression)e).Left);
            list.Add(rootFactory);

            var lambda = Expression.Lambda<Func<w.FrameworkElementFactory>>(
                Expression.Block(variables, list));

            return lambda.Compile()();
        }

        static ConstructorInfo ci = ReflectionTools.GetConstuctorInfo(() => new w.FrameworkElementFactory(typeof(Border)));
        static MethodInfo miAddHandler = ReflectionTools.GetMethodInfo((w.FrameworkElementFactory fef) => fef.AddHandler(null, null));
        static MethodInfo miRemoveHandler = ReflectionTools.GetMethodInfo((w.FrameworkElementFactory fef) => fef.RemoveHandler(null, null));
        static MethodInfo miSetBinding = ReflectionTools.GetMethodInfo((w.FrameworkElementFactory fef) => fef.SetBinding(null, null));
        static MethodInfo miSetValue = ReflectionTools.GetMethodInfo((w.FrameworkElementFactory fef) => fef.SetValue(null, null));
        static MethodInfo miSetResourceReference = ReflectionTools.GetMethodInfo((w.FrameworkElementFactory fef) => fef.SetResourceReference(null, null));
        static MethodInfo miAppendChild = ReflectionTools.GetMethodInfo((w.FrameworkElementFactory fef) => fef.AppendChild(null));

        static List<Expression> Process(Expression expression, ParameterExpression factory)
        {
            if (expression.NodeType == ExpressionType.New)
            {
                if(((NewExpression)expression).Arguments.Any())
                    throw new InvalidOperationException("No arguments in constructo allowed");
 
                return new List<Expression>
                {
                    Expression.Assign(factory, Expression.New(ci, Expression.Constant(expression.Type)))
                }; 
            }

            if(expression.NodeType == ExpressionType.MemberInit)
            {
                MemberInitExpression mie = (MemberInitExpression)expression;
                var list = Process(mie.NewExpression, factory);
                
                foreach (MemberBinding mb in mie.Bindings)
                {
                    switch (mb.BindingType)
                    {
                        case MemberBindingType.Assignment:
                            {
                                MemberAssignment ma = (MemberAssignment)mb;

                                PropertyInfo pi = ma.Member as PropertyInfo;

                                if (IsDefaultMember(pi))
                                    list.AddRange(ProcessChild(ma.Expression, factory));
                                else
                                {
                                    DependencyPropertyDescriptor desc = DependencyPropertyDescriptor.FromName(pi.Name, pi.DeclaringType, pi.DeclaringType);
                                    if (desc == null)
                                        throw new InvalidOperationException("{0} is not a DependencyProperty".Formato(pi.PropertyName()));

                                    list.Add(Expression.Call(factory, miSetValue, Expression.Constant(desc.DependencyProperty), Expression.Convert(ma.Expression, typeof(object))));
                                }
                            }break;
                        case MemberBindingType.ListBinding:
                            {
                                MemberListBinding bindings = (MemberListBinding)mb;

                                DefaultMemberAttribute dma = bindings.Member.DeclaringType.GetCustomAttribute<DefaultMemberAttribute>();

                                //if(!IsDefaultMember(bindings.Member))
                                //    throw new InvalidOperationException("Add items only work for the DefaultMember");

                                foreach (var item in bindings.Initializers)
                                {
                                    if(item.Arguments.Count != 1)
                                        throw new InvalidOperationException("Add Method {0} not supported".Formato(item.AddMethod.MethodName()));

                                    list.AddRange(ProcessChild(item.Arguments.SingleEx(), factory));
                                }

                            }break;
                        case MemberBindingType.MemberBinding:
                            throw new InvalidOperationException("MemberBinding not supported"); 
                    }
                }

                return list;
            }

            if (expression.NodeType == ExpressionType.Call)
            {
                MethodCallExpression call = (MethodCallExpression)expression;

                MethodInfo mi = call.Method;

                if (mi.DeclaringType != typeof(Fluent) || mi.ReturnType != mi.GetParameters()[0].ParameterType)
                    throw new InvalidOperationException("Method {0} not supported".Formato(mi.MethodName()));

                var list = Process(call.Arguments[0], factory);

                list.Add(ProcessMethod(factory, call));

                return list;
            }

            if (expression.NodeType == ExpressionType.Convert)
            {
                return Process(((UnaryExpression)expression).Operand, factory); 
            }

            throw new InvalidOperationException("Expression {0} not supported");
        }

        private static List<Expression> ProcessChild(Expression single, ParameterExpression factory)
        {
            if (!typeof(w.FrameworkElement).IsAssignableFrom(single.Type) && !typeof(w.FrameworkContentElement).IsAssignableFrom(single.Type))
                throw new InvalidOperationException("Can not make a {0} from a {1}".Formato(typeof(w.FrameworkElementFactory).Name, single.Type));

            ParameterExpression newFactory = Expression.Parameter(typeof(w.FrameworkElementFactory));
            var list = Process(single, newFactory);
            list.Add(Expression.Call(factory, miAppendChild, newFactory));
            return list;
        }

        private static bool IsDefaultMember(MemberInfo mi)
        {
            var dma = mi.DeclaringType.GetCustomAttribute<ContentPropertyAttribute>();
            return dma != null && dma.Name == mi.Name;
        }

        static Expression ProcessMethod(ParameterExpression factory, MethodCallExpression call)
        {
            switch (call.Method.Name)
            {
                case "Set": return Expression.Call(factory, miSetValue, call.Arguments[1], call.Arguments[2]);
                case "Hide": return Expression.Call(factory, miSetValue, Expression.Constant(w.UIElement.VisibilityProperty), Expression.Constant(w.Visibility.Hidden));
                case "Collapse": return Expression.Call(factory, miSetValue, Expression.Constant(w.UIElement.VisibilityProperty), Expression.Constant(w.Visibility.Collapsed));
                case "Visible": return Expression.Call(factory, miSetValue, Expression.Constant(w.UIElement.VisibilityProperty), Expression.Constant(w.Visibility.Visible));
                case "ReadOnly": return Expression.Call(factory, miSetValue, Expression.Constant(Common.IsReadOnlyProperty), Expression.Constant(true));
                case "Editable": return Expression.Call(factory, miSetValue, Expression.Constant(Common.IsReadOnlyProperty), Expression.Constant(false));

                case "Handle": return Expression.Call(factory, miAddHandler, call.Arguments[1], Expression.Convert(call.Arguments[2], typeof(Delegate)));
                case "ResourceReference": return Expression.Call(factory, miSetResourceReference, call.Arguments[1], call.Arguments[2]);

                case "Bind": return Expression.Call(factory, miSetBinding, call.Arguments[1], GetBinding(call.Method.GetGenericArguments().Length == 2, call.Arguments.Skip(2).ToArray())); 
            }

            throw new InvalidOperationException("Methods {0} not supported".Formato(call.Method.Name)); 
        }

        static ConstructorInfo ciBinding = ReflectionTools.GetConstuctorInfo(() => new Binding(""));
        static PropertyInfo ciSource = ReflectionTools.GetPropertyInfo((Binding b) => b.Source);
        static PropertyInfo ciConverter = ReflectionTools.GetPropertyInfo((Binding b) => b.Converter);

        static Expression GetBinding(bool hasExpression, Expression[] expression)
        {
            if (typeof(BindingBase).IsAssignableFrom(expression[0].Type))
                return expression[0];

            bool noSource = hasExpression? typeof(LambdaExpression).IsAssignableFrom(expression[0].Type) : 
                                           typeof(string).IsAssignableFrom(expression[0].Type); 

            Expression path = hasExpression?  (noSource ? GetPathConstant(expression[0]): GetPathConstant(expression[1])): 
                                              (noSource ? expression[0]: expression[1]);


            Expression source = noSource? null: expression[0];

            Expression converter = expression.Length == (noSource? 2:3) ? expression.Last(): null;

            NewExpression newExpr = Expression.New(ciBinding, path);

            if (source == null && converter == null)
                return newExpr;

            List<MemberBinding> binding = new List<MemberBinding>();
            if (source != null)
                binding.Add(Expression.Bind(ciSource, source));

            if (converter != null)
                binding.Add(Expression.Bind(ciConverter, converter));


            return Expression.MemberInit(newExpr, binding);

        }

        static ConstantExpression GetPathConstant(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Quote)
                expression = ((UnaryExpression)expression).Operand;

            if (expression.NodeType != ExpressionType.Lambda)
                throw new InvalidOperationException();

            string str = RouteVisitor.GetRoute((LambdaExpression)expression);

            return Expression.Constant(str); 
        }
    }
}
