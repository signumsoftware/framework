using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Localization
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

        public override string ToString()
        {
            return user.TryToString();
        }
    }

    [Serializable]
    public class TranslatedCultureDN : EmbeddedEntity
    {
        CultureInfoDN culture;
        public CultureInfoDN Culture
        {
            get { return culture; }
            set { Set(ref culture, value, () => Culture); }
        }

        TranslatedCultureType type;
        public TranslatedCultureType Type
        {
            get { return type; }
            set { Set(ref type, value, () => Type); }
        }
    }

    public enum TranslatedCultureType
    {
        Target,
        Reference,
    }

    public enum FooOperation
    {
        Save
    }

    public enum TranslatorOperation
    {
        Save
    }
}
