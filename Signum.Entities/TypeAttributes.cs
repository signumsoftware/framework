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
    public sealed class EntityKindAttribute : Attribute
    {
        public EntityKind EntityType { get; private set; }

        public EntityKindAttribute(EntityKind entityType)
        {
            this.EntityType = entityType;
        }
    }

    
    public enum EntityKind
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