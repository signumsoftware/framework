using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class Reactive : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UseSessionWhenNew : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class LowPopulationAttribute : Attribute
    {
        bool low = true;

        public bool Low
        {
            get { return low; }
            set { low = value; }
        }

        public LowPopulationAttribute(bool low)
        {
            this.Low = low;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CleanTypeNameAttribute: Attribute
    {
        public string Name { get; private set; }
        public CleanTypeNameAttribute(string name)
        {
            this.Name = name; 
        }
    }
}