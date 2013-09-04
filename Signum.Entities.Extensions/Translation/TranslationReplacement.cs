using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Translation
{

    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class TranslationReplacementDN : Entity
    {
        [NotNullable]
        CultureInfoDN cultureInfo;
        [NotNullValidator]
        public CultureInfoDN CultureInfo
        {
            get { return cultureInfo; }
            set { Set(ref cultureInfo, value, () => CultureInfo); }
        }

        [NotNullable, SqlDbType(Size = 200)]
        string wrongTranslation;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string WrongTranslation
        {
            get { return wrongTranslation; }
            set { Set(ref wrongTranslation, value, () => WrongTranslation); }
        }

        [NotNullable, SqlDbType(Size = 200)]
        string rightTranslation;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string RightTranslation
        {
            get { return rightTranslation; }
            set { Set(ref rightTranslation, value, () => RightTranslation); }
        }
    }

    public enum TranslationReplacementOperation
    {
        Save,
        Delete
    }
}
