using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Signum.Entities.Translation;
using Signum.Utilities;

namespace Signum.Web.Translation
{
    public class TranslationClient
    {
        public static string ViewPrefix = "~/Translation/Views/{0}.cshtml";

        public static void Start(params Type[] types)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(TranslationClient));
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<TranslatorDN>{ PartialViewName = t=>ViewPrefix.Formato("Translator")},
                    new EmbeddedEntitySettings<TranslatedCultureDN>{ PartialViewName = t=>ViewPrefix.Formato("Translator")},
                });
            }
        }
    }
}