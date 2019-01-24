using System;
using System.Collections.Generic;
using System.Text;


namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class AssertsTrueAttribute : Attribute
    {
        public AssertsTrueAttribute() { }
    }
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class AssertsFalseAttribute : Attribute
    {
        public AssertsFalseAttribute() { }
    }
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class EnsuresNotNullAttribute : Attribute
    {
        public EnsuresNotNullAttribute() { }
    }
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class NotNullWhenFalseAttribute : Attribute
    {
        public NotNullWhenFalseAttribute() { }
    }
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class NotNullWhenTrueAttribute : Attribute
    {
        public NotNullWhenTrueAttribute() { }
    }
}
