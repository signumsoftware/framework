using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;

namespace Signum.Entities.Translation
{

    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class TranslationReplacementEntity : Entity
    {
        [NotNullable]
        CultureInfoEntity cultureInfo;
        [NotNullValidator]
        public CultureInfoEntity CultureInfo
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
        public static readonly ExecuteSymbol<TranslationReplacementEntity> Save = OperationSymbol.Execute<TranslationReplacementEntity>();
        public static readonly DeleteSymbol<TranslationReplacementEntity> Delete = OperationSymbol.Delete<TranslationReplacementEntity>();
    }
}
