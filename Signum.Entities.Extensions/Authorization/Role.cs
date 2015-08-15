using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Security.Authentication;
using System.Linq.Expressions;
using System.Collections.Specialized;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class RoleEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 100)]
        public string Name { get; set; }

        MergeStrategy mergeStrategy;
        public MergeStrategy MergeStrategy
        {
            get { return mergeStrategy; }
            set
            {
                if (Set(ref mergeStrategy, value))
                    Notify(() => StrategyHint);
            }
        }

        [NotNullable, NotifyCollectionChanged]
        public MList<Lite<RoleEntity>> Roles { get; set; } = new MList<Lite<RoleEntity>>();

        protected override void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            Notify(() => StrategyHint);
        }

        [HiddenProperty]
        public string StrategyHint
        {
            get
            {
                if (Roles.Any())
                    return null;

                return "No Roles -> " + (mergeStrategy == MergeStrategy.Union ? AuthAdminMessage.Nothing : AuthAdminMessage.Everything).NiceToString();
            }
        }

        static Expression<Func<RoleEntity, string>> ToStringExpression = e => e.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public static RoleEntity Current
        {
            get
            {
                UserEntity user = UserEntity.Current;
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

    [AutoInit]
    public static class RoleOperation
    {
        public static ExecuteSymbol<RoleEntity> Save;
        public static DeleteSymbol<RoleEntity> Delete;
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Master), TicksColumn(false)]
    public class LastAuthRulesImportEntity : Entity
    {
        [UniqueIndex, FieldWithoutProperty]
        string uniqueKey = "Unique";

        public DateTime Date { get; set; }

        static Expression<Func<LastAuthRulesImportEntity, string>> ToStringExpression = e => e.uniqueKey;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
