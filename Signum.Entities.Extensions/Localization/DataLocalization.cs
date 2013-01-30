using Signum.Entities.Basics;
using Signum.Entities.Localization;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Extensions.Localization
{
    [Serializable, EntityKind(EntityKind.Main)]
    public class DataLocalizationDN : Entity
    {
        [NotNullable]
        CultureInfoDN culture;
        [NotNullValidator]
        public CultureInfoDN Culture
        {
            get { return culture; }
            set { Set(ref culture, value, () => Culture); }
        }

        [ImplementedByAll]
        Lite<IdentifiableEntity> instance;
        [NotNullValidator]
        public Lite<IdentifiableEntity> Instance
        {
            get { return instance; }
            set { Set(ref instance, value, () => Instance); }
        }

        [NotNullable]
        Lite<PropertyRouteDN> propertyRoute;
        [NotNullValidator]
        public Lite<PropertyRouteDN> PropertyRoute
        {
            get { return propertyRoute; }
            set { Set(ref propertyRoute, value, () => PropertyRoute); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string localizedText;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string LocalizedText
        {
            get { return localizedText; }
            set { Set(ref localizedText, value, () => LocalizedText); }
        }

        public override string ToString()
        {
            return "{0} - ({1} {2}).{3}".Formato(culture, instance.TryCC(e => e.EntityType.Name), instance.TryCS(e => e.IdOrNull), propertyRoute);
        }
    }

    public enum DataLocalizationOperation
    {
        Save,
        Delete,
    }
}
