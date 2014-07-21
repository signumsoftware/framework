using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;

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
            set { Set(ref cultureInfo, value); }
        }

        [NotNullable, SqlDbType(Size = 200)]
        string wrongTranslation;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string WrongTranslation
        {
            get { return wrongTranslation; }
            set { Set(ref wrongTranslation, value); }
        }

        [NotNullable, SqlDbType(Size = 200)]
        string rightTranslation;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string RightTranslation
        {
            get { return rightTranslation; }
            set { Set(ref rightTranslation, value); }
        }
    }

    public static class TranslationReplacementOperation
    {
        public static readonly ExecuteSymbol<TranslationReplacementDN> Save = OperationSymbol.Execute<TranslationReplacementDN>();
        public static readonly DeleteSymbol<TranslationReplacementDN> Delete = OperationSymbol.Delete<TranslationReplacementDN>();
    }
}
