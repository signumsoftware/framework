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
        public abstract string IsAllowed();
    }

    [Serializable]
    public class CleanMeta : Meta
    {
        public PropertyRoute[] PropertyRoutes;

        public CleanMeta(PropertyRoute[] propertyRoutes)
        {
            this.PropertyRoutes = propertyRoutes;
        }

        public override string IsAllowed()
        {
            var result = PropertyRoutes.Select(a => a.IsAllowed()).NotNull().CommaAnd();
            if (string.IsNullOrEmpty(result))
                return null;

            return result;
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

        public override string IsAllowed()
        {
            var result = Properties.Select(a => a.IsAllowed()).NotNull().CommaAnd();
            if (string.IsNullOrEmpty(result))
                return null;

            return result;
        }

        public override string ToString()
        {
            return "DirtyMeta({0})".Formato(Properties.ToString(", "));
        }
    }
}
