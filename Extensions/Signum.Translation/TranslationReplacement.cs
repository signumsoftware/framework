using Signum.Basics;

namespace Signum.Translation;


[EntityKind(EntityKind.Main, EntityData.Master)]
public class TranslationReplacementEntity : Entity
{
    
    public CultureInfoEntity CultureInfo { get; set; }

    [StringLengthValidator(Min = 3, Max = 200)]
    public string WrongTranslation { get; set; }

    [StringLengthValidator(Min = 3, Max = 200)]
    public string RightTranslation { get; set; }
}

[AutoInit]
public static class TranslationReplacementOperation
{
    public static ExecuteSymbol<TranslationReplacementEntity> Save;
    public static DeleteSymbol<TranslationReplacementEntity> Delete;
}
