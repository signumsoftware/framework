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
    [Serializable, EntityKind(EntityKind.String)]
    public class TranslatorDN : Entity
    {
        [NotNullable, UniqueIndex, ImplementedBy(typeof(UserDN))]
        Lite<IUserDN> user;
        [NotNullValidator]
        public Lite<IUserDN> User
        {
            get { return user; }
            set { Set(ref user, value, () => User); }
        }

        [NotNullable]
        MList<TranslatedCultureDN> cultures = new MList<TranslatedCultureDN>();
        [NotNullValidator, NoRepeatValidator]
        public MList<TranslatedCultureDN> Cultures
        {
            get { return cultures; }
            set { Set(ref cultures, value, () => Cultures); }
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => Cultures))
            {
                var error = Cultures.GroupBy(a => a.Culture).Where(a => a.Count() > 1).ToString(a => a.Key.ToString(), ", ");

                if (error.HasText())
                    return TranslationMessages.RepeatedCultures0.NiceToString().Formato(error); 
            }

            return base.PropertyValidation(pi);
        }

        public override string ToString()
        {
            return user.TryToString();
        }
    }

    [Serializable]
    public class TranslatedCultureDN : EmbeddedEntity
    {
        [NotNullable]
        CultureInfoDN culture;
        [NotNullValidator]
        public CultureInfoDN Culture
        {
            get { return culture; }
            set { Set(ref culture, value, () => Culture); }
        }

        TranslatedCultureAction action;
        public TranslatedCultureAction Action
        {
            get { return action; }
            set { Set(ref action, value, () => Action); }
        }
    }

    public enum TranslatedCultureAction
    {
        Translate,
        Read,
    }


    public enum TranslatorOperation
    {
        Save,
        Delete,
    }

    public enum TranslationMessages
    {
        [Description("Repeated cultures {0}")]
        RepeatedCultures0
    }
}
