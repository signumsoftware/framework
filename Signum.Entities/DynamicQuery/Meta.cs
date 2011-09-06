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
        public abstract bool IsAllowed();
    }

    [Serializable]
    public class CleanMeta : Meta
    {
        public FieldRoute[] PropertyRoutes;

        public CleanMeta(FieldRoute[] propertyRoutes)
        {
            this.PropertyRoutes = propertyRoutes;
        }

        public override bool IsAllowed()
        {
            return PropertyRoutes.All(a => a.IsAllowed());
        }

        public override string ToString()
        {
            return "CleanMeta({0})".Formato(PropertyRoutes.ToString(", "));
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

        public override bool IsAllowed()
        {
            return Properties.All(cm => cm.IsAllowed());
        }

        public override string ToString()
        {
            return "DirtyMeta({0})".Formato(Properties.ToString(", "));
        }
    }
}
