using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.ObjectModel;
using Signum.Utilities;
using System.Collections;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public abstract class Meta
    {

    }

    [Serializable]
    public class CleanMeta : Meta
    {
        public readonly Type Type;
        public readonly MemberInfo Member;
        public readonly CleanMeta Parent;

        public CleanMeta(Type type, MemberInfo member, CleanMeta parent)
        {
            this.Type = type;
            this.Member = member;
            this.Parent = parent;
        }

        public string PropertyName
        {
            get
            {
                if(ReflectionTools.CollectionType(Type)!= null && Member.Name == "Items")
                    return Parent.PropertyName + "/"; 

                if(Parent != null)
                    return Parent.PropertyName + "." + Member.Name;

                return Member.Name; 
            }
        }
    }

    [Serializable]
    public class DirtyMeta : Meta
    {
        public readonly ReadOnlyCollection<CleanMeta> Properties;

        public DirtyMeta(Meta[] properties)
        {
            Properties = properties.OfType<CleanMeta>().Concat(
                properties.OfType<DirtyMeta>().SelectMany(d => d.Properties))
                .ToReadOnly();
        }
    }
}
