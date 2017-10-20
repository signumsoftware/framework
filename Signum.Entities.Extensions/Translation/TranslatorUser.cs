using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Translation
{
    [AutoInit]
    public static class TranslationPermission
    {
        public static PermissionSymbol TranslateCode;
        public static PermissionSymbol TranslateInstances;
    }

    public enum TranslationMessage
    {
        [Description("Repeated cultures {0}")]
        RepeatedCultures0,

        CodeTranslations,
        InstanceTranslations,

        [Description("Synchronize {0} in {1}")]
        Synchronize0In1,

        [Description("View {0} in {1}")]
        View0In1,

        [Description("all languages")]
        AllLanguages,

        [Description("{0} already synchronized")]
        _0AlreadySynchronized,

        NothingToTranslate,
        All,

        [Description("Nothing to translate in {0}")]
        NothingToTranslateIn0,

        [Description("sync")]
        Sync,

        [Description("view")]
        View,

        [Description("none")]
        None,

        [Description("edit")]
        Edit,

        Member,
        Type,

        Instance,
        Property,
        Save,
        Search,
        [Description("Press search for results...")]
        PressSearchForResults,
        NoResultsFound,

        Namespace, 
        NewTypes, 
        NewTranslations, 

        BackToTranslationStatus,
    }

    public enum TranslationJavascriptMessage
    {
        WrongTranslationToSubstitute,
        RightTranslation,
        RememberChange,
    }

}
