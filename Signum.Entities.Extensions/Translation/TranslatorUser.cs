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
    public class TranslatorUserDN : Entity
    {
        [NotNullable, UniqueIndex, ImplementedBy(typeof(UserDN))]
        Lite<IUserDN> user;
        [NotNullValidator]
        public Lite<IUserDN> User
        {
            get { return user; }
            set { Set(ref user, value); }
        }

        [NotNullable, PreserveOrder]
        MList<TranslatorUserCultureDN> cultures = new MList<TranslatorUserCultureDN>();
        [NotNullValidator, NoRepeatValidator]
        public MList<TranslatorUserCultureDN> Cultures
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
                    return TranslationMessage.RepeatedCultures0.NiceToString().Formato(error); 
            }

            return base.PropertyValidation(pi);
        }

        public override string ToString()
        {
            return user.TryToString();
        }
    }

    [Serializable]
    public class TranslatorUserCultureDN : EmbeddedEntity
    {
        [NotNullable]
        CultureInfoDN culture;
        [NotNullValidator]
        public CultureInfoDN Culture
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
        public static readonly ExecuteSymbol<TranslatorUserDN> Save = OperationSymbol.Execute<TranslatorUserDN>();
        public static readonly DeleteSymbol<TranslatorUserDN> Delete = OperationSymbol.Delete<TranslatorUserDN>();
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
