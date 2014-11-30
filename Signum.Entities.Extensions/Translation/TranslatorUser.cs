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
        Lite<IUserEntity> user;
        [NotNullValidator]
        public Lite<IUserEntity> User
        {
            get { return user; }
            set { Set(ref user, value); }
        }

        [NotNullable, PreserveOrder]
        MList<TranslatorUserCultureEntity> cultures = new MList<TranslatorUserCultureEntity>();
        [NotNullValidator, NoRepeatValidator]
        public MList<TranslatorUserCultureEntity> Cultures
        {
            get { return cultures; }
            set { Set(ref cultures, value); }
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => Cultures))
            {
                var error = Cultures.GroupBy(a => a.Culture).Where(a => a.Count() > 1).ToString(a => a.Key.ToString(), ", ");

                if (error.HasText())
                    return TranslationMessage.RepeatedCultures0.NiceToString().FormatWith(error); 
            }

            return base.PropertyValidation(pi);
        }

        public override string ToString()
        {
            return user.TryToString();
        }
    }

    [Serializable]
    public class TranslatorUserCultureEntity : EmbeddedEntity
    {
        [NotNullable]
        CultureInfoEntity culture;
        [NotNullValidator]
        public CultureInfoEntity Culture
        {
            get { return culture; }
            set { Set(ref culture, value); }
        }

        TranslatedCultureAction action;
        public TranslatedCultureAction Action
        {
            get { return action; }
            set { Set(ref action, value); }
        }
    }

    public enum TranslatedCultureAction
    {
        Translate,
        Read,
    }

    public static class TranslationPermission
    {
        public static readonly PermissionSymbol TranslateCode = new PermissionSymbol();
        public static readonly PermissionSymbol TranslateInstances = new PermissionSymbol();
    }

    public static class TranslatorUserOperation
    {
        public static readonly ExecuteSymbol<TranslatorUserEntity> Save = OperationSymbol.Execute<TranslatorUserEntity>();
        public static readonly DeleteSymbol<TranslatorUserEntity> Delete = OperationSymbol.Delete<TranslatorUserEntity>();
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
