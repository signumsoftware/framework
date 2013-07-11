using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Security.Authentication;
using System.Linq.Expressions;
using System.Collections.Specialized;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class RoleDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        MergeStrategy mergeStrategy;
        public MergeStrategy MergeStrategy
        {
            get { return mergeStrategy; }
            set
            {
                if (Set(ref mergeStrategy, value, () => MergeStrategy))
                    Notify(() => StrategyHint);
            }
        }
    
        [NotNullable, NotifyCollectionChanged]
        MList<Lite<RoleDN>> roles = new MList<Lite<RoleDN>>();
        public MList<Lite<RoleDN>> Roles
        {
            get { return roles; }
            set { Set(ref roles, value, () => Roles); }
        }

        protected override void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            Notify(() => StrategyHint); 
        }

        [HiddenProperty]
        public string StrategyHint
        {
            get
            {
                if (roles.Any())
                    return null;

                return ("(-> " + (mergeStrategy == MergeStrategy.Union ? AuthAdminMessage.Nothing : AuthAdminMessage.Everything).NiceToString() + ")");
            }
        }

        static readonly Expression<Func<RoleDN, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public static RoleDN Current
        {
            get
            {
                UserDN user = UserDN.Current;
                if (user == null)
                    throw new AuthenticationException(AuthMessage.NotUserLogged.NiceToString());

               return user.Role;
            }
        }
    }

    public enum MergeStrategy
    {
        Union,
        Intersection,
    }

    public enum RoleQuery
    {
        RolesReferedBy
    }

    public enum RoleOperation
    {
        Save,
        Delete
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Master)]
    public class LastAuthRulesImportDN : IdentifiableEntity
    {
        [UniqueIndex, FieldWithoutProperty]
        string uniqueKey = "Unique";

        DateTime date;
        public DateTime Date
        {
            get { return date; }
            set { Set(ref date, value, () => Date); }
        }

        static Expression<Func<LastAuthRulesImportDN, string>> ToStringExpression = e => e.uniqueKey;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
