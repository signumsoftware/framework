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
    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class TranslatorUserEntity : Entity
    {
        [NotNullable, UniqueIndex, ImplementedBy(typeof(UserEntity))]
        [NotNullValidator]
        public Lite<IUserEntity> User { get; set; }

        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<TranslatorUserCultureEntity> Cultures { get; set; } = new MList<TranslatorUserCultureEntity>();

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Cultures))
            {
                var error = Cultures.GroupBy(a => a.Culture).Where(a => a.Count() > 1).ToString(a => a.Key.ToString(), ", ");

                if (error.HasText())
                    return TranslationMessage.RepeatedCultures0.NiceToString().FormatWith(error);
            }

            return base.PropertyValidation(pi);
        }

        public override string ToString()
        {
            return User?.ToString();
        }
    }

    [Serializable]
    public class TranslatorUserCultureEntity : EmbeddedEntity
    {
        [NotNullable]
        [NotNullValidator]
        public CultureInfoEntity Culture { get; set; }

        public TranslatedCultureAction Action { get; set; }
    }

    public enum TranslatedCultureAction
    {
        Translate,
        Read,
    }

    [AutoInit]
    public static class TranslationPermission
    {
        public static PermissionSymbol TranslateCode;
        public static PermissionSymbol TranslateInstances;
    }

    [AutoInit]
    public static class TranslatorUserOperation
    {
        public static ExecuteSymbol<TranslatorUserEntity> Save;
        public static DeleteSymbol<TranslatorUserEntity> Delete;
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
    }

    public enum TranslationJavascriptMessage
    {
        WrongTranslationToSubstitute,
        RightTranslation,
        RememberChange,
    }

}
