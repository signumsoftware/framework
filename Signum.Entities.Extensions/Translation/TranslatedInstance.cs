using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        PropertyRouteDN propertyRoute;
        [NotNullValidator]
        public PropertyRouteDN PropertyRoute
        {
            get { return propertyRoute; }
            set { Set(ref propertyRoute, value, () => PropertyRoute); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string translatedText;
        [StringLengthValidator(AllowNulls = false)]
        public string TranslatedText
        {
            get { return translatedText; }
            set { Set(ref translatedText, value, () => TranslatedText); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string originalText;
        [StringLengthValidator(AllowNulls = false)]
        public string OriginalText
        {
            get { return originalText; }
            set { Set(ref originalText, value, () => OriginalText); }
        }

        public override string ToString()
        {
            return "{0} {1} {2}".Formato(culture, instance, propertyRoute);
        }
    }
}
