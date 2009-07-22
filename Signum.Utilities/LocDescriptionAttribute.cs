using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Resources;

namespace Signum.Utilities
{
    [AttributeUsage(AttributeTargets.All)]
    public class LocDescriptionAttribute : DescriptionAttribute
    {
        public bool Auto { get { return resourceKey == null || resourceSource == null; } }

        Type resourceSource;
        string resourceKey;

        public LocDescriptionAttribute()
        {
        }

        public LocDescriptionAttribute(Type resourceSource, string resourceKey)
            : base()
        {
            this.resourceSource = resourceSource;
            this.resourceKey = resourceKey;
        }

        public override string Description
        {
            get
            {
                if (Auto)
                    throw new ApplicationException("Use ReflectionTools.GetDescription instead");

                return new ResourceManager(resourceSource).GetString(resourceKey);
            }
        }
    }
}
