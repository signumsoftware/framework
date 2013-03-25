using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Signum.Engine.Authorization;
using Signum.Entities.Translation;
using Signum.Utilities;
using Signum.Web.Omnibox;
using Signum.Web.Translation.Controllers;

namespace Signum.Web.Translation
{
    public class TranslationClient
    {
        public static string ViewPrefix = "~/Translation/Views/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(TranslationClient));
                Navigator.AddSettings(new List<EntitySettings>
                {   
                    new EntitySettings<CultureInfoDN>{ PartialViewName = t=>ViewPrefix.Formato("CultureInfoView")},
                     new EntitySettings<TranslatorDN>{ PartialViewName = t=>ViewPrefix.Formato("Translator")},
                    new EmbeddedEntitySettings<TranslatedCultureDN>{ PartialViewName = t=>ViewPrefix.Formato("Translator")},
                });

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("Translation",
                    () => TranslationPermission.TranslateCode.IsAuthorized(),
                    uh => uh.Action((TranslationController tc) => tc.Index())));
            }
        }
    }
}