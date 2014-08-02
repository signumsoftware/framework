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
    public class TranslatedInstanceDN : Entity
    {
        [NotNullable]
        CultureInfoDN culture;
        [NotNullValidator]
        public CultureInfoDN Culture
        {
            get { return culture; }
            set { Set(ref culture, value); }
        }

        [ImplementedByAll]
        Lite<IdentifiableEntity> instance;
        [NotNullValidator]
        public Lite<IdentifiableEntity> Instance
        {
            get { return instance; }
            set { Set(ref instance, value); }
        }

        [NotNullable]
        PropertyRouteDN propertyRoute;
        [NotNullValidator]
        public PropertyRouteDN PropertyRoute
        {
            get { return propertyRoute; }
            set { Set(ref propertyRoute, value); }
        }

        int? rowId;
        public int? RowId
        {
            get { return rowId; }
            set { Set(ref rowId, value); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string translatedText;
        [StringLengthValidator(AllowNulls = false)]
        public string TranslatedText
        {
            get { return translatedText; }
            set { Set(ref translatedText, value); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string originalText;
        [StringLengthValidator(AllowNulls = false)]
        public string OriginalText
        {
            get { return originalText; }
            set { Set(ref originalText, value); }
        }

        public override string ToString()
        {
            return "{0} {1} {2}".Formato(culture, instance, propertyRoute);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => RowId) && PropertyRoute != null)
            {
                if (RowId == null && PropertyRoute.Path.Contains("/"))
                    return "{0} should be set for route {1}".Formato(pi.NiceName(), PropertyRoute);

                if (RowId != null && !PropertyRoute.Path.Contains("/"))
                    return "{0} should be null for route {1}".Formato(pi.NiceName(), PropertyRoute);
            }

            return null;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TranslateFieldAttribute : Attribute
    {
        public TraducibleRouteType TraducibleRouteType = TraducibleRouteType.Text;
        
      
    }

    public enum TraducibleRouteType
    {
        Text,
        Html
    }
}
