using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Signum.Entities.Translation
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Master)]
    public class TranslatedInstanceEntity : Entity
    {
        [NotNullValidator]
        public CultureInfoEntity Culture { get; set; }

        [ImplementedByAll]
        [NotNullValidator]
        public Lite<Entity> Instance { get; set; }

        [NotNullValidator]
        public PropertyRouteEntity PropertyRoute { get; set; }

        public string RowId { get; set; }

        [StringLengthValidator(AllowNulls = false, MultiLine = true)]
        public string TranslatedText { get; set; }

        [StringLengthValidator(AllowNulls = false, MultiLine = true)]
        public string OriginalText { get; set; }

        public override string ToString()
        {
            return "{0} {1} {2}".FormatWith(Culture, Instance, PropertyRoute);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(RowId) && PropertyRoute != null)
            {
                if (RowId == null && PropertyRoute.Path.Contains("/"))
                    return "{0} should be set for route {1}".FormatWith(pi.NiceName(), PropertyRoute);

                if (RowId != null && !PropertyRoute.Path.Contains("/"))
                    return "{0} should be null for route {1}".FormatWith(pi.NiceName(), PropertyRoute);
            }

            return null;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class TranslateFieldAttribute : Attribute
    {
        public TranslateableRouteType TranslatableRouteType = TranslateableRouteType.Text;


    }

    public enum TranslateableRouteType
    {
        Text,
        Html
    }
}
