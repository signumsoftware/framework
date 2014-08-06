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
                sb.Include<TranslationReplacementDN>();

                sb.AddUniqueIndex<TranslationReplacementDN>(tr => new { tr.CultureInfo, tr.WrongTranslation });

                dqm.RegisterQuery(typeof(TranslationReplacementDN), () =>
                   from e in Database.Query<TranslationReplacementDN>()
                   select new
                   {
                       Entity = e,
                       e.Id,
                       e.CultureInfo,
                       e.WrongTranslation,
                       e.RightTranslation,
                   });

                new Graph<TranslationReplacementDN>.Execute(TranslationReplacementOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { },
                }.Register();

                new Graph<TranslationReplacementDN>.Delete(TranslationReplacementOperation.Delete)
                {
                    Delete = (e, _) => { e.Delete(); },
                }.Register();

                ReplacementsLazy = sb.GlobalLazy(() => Database.Query<TranslationReplacementDN>()
                    .AgGroupToDictionary(a => a.CultureInfo.ToCultureInfo(),
                    gr =>
                    {
                        var dic = gr.ToDictionary(a => a.WrongTranslation, a => a.RightTranslation, StringComparer.InvariantCultureIgnoreCase, "wrong translations");

                        var regex = new Regex(dic.Keys.ToString(Regex.Escape, "|"), RegexOptions.IgnoreCase);

                        return new TranslationReplacementPack { Dictionary = dic, Regex = regex };
                    }),
                    new InvalidateWith(typeof(TranslationReplacementDN)));

            }
        }

        public static void ReplacementFeedback(CultureInfo ci, string translationWrong, string translationRight)
        {
            if (string.IsNullOrWhiteSpace(translationWrong))
                throw new ArgumentNullException(translationWrong);

            if (string.IsNullOrWhiteSpace(translationRight))
                throw new ArgumentNullException(translationRight);
            
            if (!Database.Query<TranslationReplacementDN>().Any(a => a.CultureInfo == ci.ToCultureInfoDN() && a.WrongTranslation == translationWrong))
                using (OperationLogic.AllowSave<TranslationReplacementDN>())
                    new TranslationReplacementDN
                    {
                        CultureInfo = ci.ToCultureInfoDN(),
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
