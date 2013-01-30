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
    public class CodeLocalizationDN : Entity
    {
        [NotNullable]
        Lite<CultureInfoDN> culture;
        [NotNullValidator]
        public Lite<CultureInfoDN> Culture
        {
            get { return culture; }
            set { Set(ref culture, value, () => Culture); }
        }

        CodeType codeType;
        public CodeType CodeType
        {
            get { return codeType; }
            set { Set(ref codeType, value, () => CodeType); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string typeName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string TypeName
        {
            get { return typeName; }
            set { Set(ref typeName, value, () => TypeName); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string propertyName;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string PropertyName
        {
            get { return propertyName; }
            set { Set(ref propertyName, value, () => PropertyName); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string localizedText;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string LocalizedText
        {
            get { return localizedText; }
            set { Set(ref localizedText, value, () => LocalizedText); }
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => PropertyName))
            {
                if (PropertyName.HasText() && codeType == Localization.CodeType.Type)
                    return Resources._0IsNotAllowedOnState1.Formato(pi.NiceName(), codeType.NiceToString());

                if (!PropertyName.HasText() && codeType != Localization.CodeType.Type)
                    return Resources._0IsNecessaryOnState1.Formato(pi.NiceName(), codeType.NiceToString());
            }

            return base.PropertyValidation(pi);
        }

        public override string ToString()
        {
            return "{0} - {1}".Formato(culture, ".".Combine(typeName, propertyName));
        }
    }

    public enum CodeType
    {
        Type,
        Property,
        Enum
    }

    public enum CodeLocalizationOperation
    {
        Save,
        Delete,
    }
}
