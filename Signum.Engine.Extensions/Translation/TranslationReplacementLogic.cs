using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities.Translation;
using Signum.Utilities;

namespace Signum.Engine.Translation
{
    public static class TranslationReplacementLogic
    {
        public static ResetLazy<Dictionary<CultureInfo, TranslationReplacementPack>> ReplacementsLazy;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<TranslationReplacementEntity>();

                sb.AddUniqueIndex<TranslationReplacementEntity>(tr => new { tr.CultureInfo, tr.WrongTranslation });

                dqm.RegisterQuery(typeof(TranslationReplacementEntity), () =>
                   from e in Database.Query<TranslationReplacementEntity>()
                   select new
                   {
                       Entity = e,
                       e.Id,
                       e.CultureInfo,
                       e.WrongTranslation,
                       e.RightTranslation,
                   });

                new Graph<TranslationReplacementEntity>.Execute(TranslationReplacementOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { },
                }.Register();

                new Graph<TranslationReplacementEntity>.Delete(TranslationReplacementOperation.Delete)
                {
                    Delete = (e, _) => { e.Delete(); },
                }.Register();

                ReplacementsLazy = sb.GlobalLazy(() => Database.Query<TranslationReplacementEntity>()
                    .AgGroupToDictionary(a => a.CultureInfo.ToCultureInfo(),
                    gr =>
                    {
                        var dic = gr.ToDictionary(a => a.WrongTranslation, a => a.RightTranslation, StringComparer.InvariantCultureIgnoreCase, "wrong translations");

                        var regex = new Regex(dic.Keys.ToString(Regex.Escape, "|"), RegexOptions.IgnoreCase);

                        return new TranslationReplacementPack { Dictionary = dic, Regex = regex };
                    }),
                    new InvalidateWith(typeof(TranslationReplacementEntity)));

            }
        }

        public static void ReplacementFeedback(CultureInfo ci, string translationWrong, string translationRight)
        {
            if (string.IsNullOrWhiteSpace(translationWrong))
                throw new ArgumentNullException(translationWrong);

            if (string.IsNullOrWhiteSpace(translationRight))
                throw new ArgumentNullException(translationRight);
            
            if (!Database.Query<TranslationReplacementEntity>().Any(a => a.CultureInfo == ci.ToCultureInfoEntity() && a.WrongTranslation == translationWrong))
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
    }
}
