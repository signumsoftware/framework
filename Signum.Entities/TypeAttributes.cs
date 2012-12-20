using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class LowPopulationAttribute : Attribute
    {
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

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class EntityTypeAttribute : Attribute
    {
        public EntityType EntityType { get; private set; }

        public EntityTypeAttribute(EntityType entityType)
        {
            this.EntityType = entityType;
        }
    }

    
    public enum EntityType
    {
        SystemString,
        System,
        String,
        Shared,
        Main,
        Part,
        SharedPart,
    }
}