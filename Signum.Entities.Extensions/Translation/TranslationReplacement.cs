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
        [NotNullValidator]
        public CultureInfoEntity CultureInfo { get; set; }

        [NotNullable, SqlDbType(Size = 200)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string WrongTranslation { get; set; }

        [NotNullable, SqlDbType(Size = 200)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string RightTranslation { get; set; }
    }

    [AutoInit]
    public static class TranslationReplacementOperation
    {
        public static ExecuteSymbol<TranslationReplacementEntity> Save;
        public static DeleteSymbol<TranslationReplacementEntity> Delete;
    }
}
