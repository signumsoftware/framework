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
        public PropertyRoute PropertyRoute; 

        public CleanMeta(PropertyRoute propertyRoute)
        {
            this.PropertyRoute = propertyRoute;
        }

        public override bool IsAllowed()
        {
            return PropertyRoute.IsAllowed();
        }

        static bool ColumnIsAllowed(UserColumn column)
        {
            return column.Token.IsAllowed();
        }

        public override string ToString()
        {
            return "CleanMeta( {0} )".Formato(PropertyRoute);
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
            return "DirtyMeta( {0} )".Formato(Properties.Select(a=>a.PropertyRoute).ToString(", "));
        }
    }
}
