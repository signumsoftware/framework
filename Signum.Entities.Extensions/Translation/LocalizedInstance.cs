using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Translation
{
    [Serializable, EntityKind(EntityKind.Main)]
    public class LocalizedInstanceDN : Entity
    {
        [NotNullable]
        Lite<CultureInfoDN> culture;
        [NotNullValidator]
        public Lite<CultureInfoDN> Culture
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
        MList<LocalizedInstancePropertyDN> properties = new MList<LocalizedInstancePropertyDN>();
        [NotNullValidator, NoRepeatValidator]
        public MList<LocalizedInstancePropertyDN> Properties
        {
            get { return properties; }
            set { Set(ref properties, value, () => Properties); }
        }

        public override string ToString()
        {
            return "{0} - ({1} {2})".Formato(culture, instance.TryCC(e => e.EntityType.Name));
        }
    }

    [Serializable]
    public class LocalizedInstancePropertyDN : Entity
    {
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
    }

    public enum LocalizedInstanceOperation
    {
        Save,
        Delete,
    }
}
