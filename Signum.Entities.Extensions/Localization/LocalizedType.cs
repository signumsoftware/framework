using Signum.Entities.Properties;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Signum.Entities.Localization
{
    [Serializable, EntityKind(EntityKind.Main)]
    public class LocalizedTypeDN : Entity
    {
        [NotNullable]
        Lite<CultureInfoDN> culture;
        [NotNullValidator]
        public Lite<CultureInfoDN> Culture
        {
            get { return culture; }
            set { Set(ref culture, value, () => Culture); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string typeName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string TypeName
        {
            get { return typeName; }
            set { Set(ref typeName, value, () => TypeName); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string singularName;
        [StringLengthValidator(AllowNulls = false, Min = 1)]
        public string SingularName
        {
            get { return singularName; }
            set { Set(ref singularName, value, () => SingularName); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string pluralName;
        [StringLengthValidator(AllowNulls = false, Min = 1)]
        public string PluralName
        {
            get { return pluralName; }
            set { Set(ref pluralName, value, () => PluralName); }
        }

        char? gender;
        public char? Gender
        {
            get { return gender; }
            set { Set(ref gender, value, () => Gender); }
        }

        bool isEntity;
        public bool IsEntity
        {
            get { return isEntity; }
            set { Set(ref isEntity, value, () => IsEntity); }
        }

        [NotNullable]
        MList<LocalizedPropertyDN> properties = new MList<LocalizedPropertyDN>();
        [NotNullValidator, NoRepeatValidator]
        public MList<LocalizedPropertyDN> Properties
        {
            get { return properties; }
            set { Set(ref properties, value, () => Properties); }
        }

        public override string ToString()
        {
            return "{0} - {1}".Formato(culture, ".".Combine(typeName));
        }
    }

    [Serializable]
    public class LocalizedPropertyDN : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        string propertyName;
        [StringLengthValidator(AllowNulls = false, Max = 100)]
        public string PropertyName
        {
            get { return propertyName; }
            set { Set(ref propertyName, value, () => PropertyName); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string localizedText;
        [StringLengthValidator(AllowNulls = false, Min = 1)]
        public string LocalizedText
        {
            get { return localizedText; }
            set { Set(ref localizedText, value, () => LocalizedText); }
        }
    }

    public enum LocalizedTypeOperation
    {
        Save,
        Delete,
    }
}
