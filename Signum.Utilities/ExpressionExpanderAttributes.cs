using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Utilities
{
    /// <summary>
    /// Interface for classes that can be used to convert calls to methods
    /// in LINQ expression trees.
    /// </summary>
    public interface IMethodExpander
    {
        Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi);
    }


    public class GenericMethodExpander
    {
        public LambdaExpression GenericLambdaExpression;
        public GenericMethodExpander(LambdaExpression genericLambdaExpression)
        {
            this.GenericLambdaExpression = genericLambdaExpression;
        } 
    }

    /// <summary>
    /// Attribute to define the class that should be used to convert calls to methods
    /// in LINQ expression trees
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class MethodExpanderAttribute : Attribute
    {
        private Type expanderType;
        public Type ExpanderType
        {
            get { return expanderType; }
        }

        /// <param name="type">A class that implements IMethodExpander</param>
		public MethodExpanderAttribute(Type type)
        {
            expanderType = type;
        }
    }

    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class ForceEagerEvaluationAttribute : Attribute
    {
      
    }

    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class AvoidEagerEvaluationAttribute : Attribute
    {

    }

    //The member is polymorphic and should be expanded in a latter stage
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class PolymorphicExpansionAttribute : Attribute
    {
        public PolymorphicExpansionAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class EagerBindingAttribute : Attribute
    {

    }

    /// <summary>
    /// Associates a method or property with a static field of type Expression with an equivalent definition that can be used inside IQueryable queries
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class ExpressionFieldAttribute : Attribute
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        /// <param name="name">The name of the field for the expression that defines the content. If not set, will be automatically found from the method body.</param>
        public ExpressionFieldAttribute(string name = "auto")
        {
            this.Name = name;
        }
    }
}
