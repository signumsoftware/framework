using System.Collections.Frozen;
using System.Globalization;
using System.Text.RegularExpressions;
using Signum.Translation;

namespace Signum.Translation;

public static class TranslationReplacementLogic
{
    public static ResetLazy<FrozenDictionary<CultureInfo, TranslationReplacementPack>> ReplacementsLazy = null!;

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<TranslationReplacementEntity>()
            .WithUniqueIndex(tr => new { tr.CultureInfo, tr.WrongTranslation })
            .WithSave(TranslationReplacementOperation.Save)
            .WithDelete(TranslationReplacementOperation.Delete)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.CultureInfo,
                e.WrongTranslation,
                e.RightTranslation,
            });


        ReplacementsLazy = sb.GlobalLazy(() => Database.Query<TranslationReplacementEntity>()
            .AgGroupToDictionary(a => a.CultureInfo.ToCultureInfo(),
            gr =>
            {
                var dic = gr.ToDictionaryEx(a => a.WrongTranslation, a => a.RightTranslation, StringComparer.InvariantCultureIgnoreCase, "wrong translations");

                var regex = new Regex(dic.Keys.ToString(Regex.Escape, "|"), RegexOptions.IgnoreCase);

                return new TranslationReplacementPack(dic, regex);
            }).ToFrozenDictionaryEx(),
            new InvalidateWith(typeof(TranslationReplacementEntity)));

    }

    public static void ReplacementFeedback(CultureInfo ci, string translationWrong, string translationRight)
    {
        if (string.IsNullOrWhiteSpace(translationWrong))
            throw new ArgumentNullException(translationWrong);

        if (string.IsNullOrWhiteSpace(translationRight))
            throw new ArgumentNullException(translationRight);
        
        if (!Database.Query<TranslationReplacementEntity>().Any(a => a.CultureInfo.Is(ci.ToCultureInfoEntity()) && a.WrongTranslation == translationWrong))
            using (OperationLogic.AllowSave<TranslationReplacementEntity>())
                new TranslationReplacementEntity
                {
                    CultureInfo = ci.ToCultureInfoEntity(),
                    WrongTranslation = translationWrong,
                    RightTranslation = translationRight,
                }.Save();
    }
}
public class TranslationReplacementPack
{
    public Dictionary<string, string> Dictionary;
    public Regex Regex;

    public TranslationReplacementPack(Dictionary<string, string> dictionary, Regex regex)
    {
        Dictionary = dictionary;
        Regex = regex;
    }
}
