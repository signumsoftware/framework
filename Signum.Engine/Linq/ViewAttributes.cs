using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Engine
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    sealed class SqlViewNameAttribute : Attribute
    {
        public string Name { get; private set;}

        public SqlViewNameAttribute(string name)
        {
            this.Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    sealed class SqlMethodAttribute : Attribute
    {
        public string Name { get; private set; }
        
        public SqlMethodAttribute()
        {
        }

        public SqlMethodAttribute(string name)
        {
            this.Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    sealed class SqlViewColumnAttribute : Attribute
    {
        public string Name { get; private set;}

        public SqlViewColumnAttribute(string name)
        {
            this.Name = name;
        }
    }

    public interface IView { }
}
