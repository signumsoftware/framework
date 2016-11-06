using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Dynamic
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class DynamicSqlMigrationEntity : Entity
    {
        public DateTime CreationDate { get; set; }

        [NotNullable, ImplementedBy(typeof(UserEntity))]
        [NotNullValidator]
        public Lite<IUserEntity> CreatedBy { get; set; }

        public DateTime? ExecutionDate { get; set; }
        
        [ImplementedBy(typeof(UserEntity))]
        public Lite<IUserEntity> ExecutedBy { get; set; }

        [NotNullable, SqlDbType(Size = 200)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string Comment { get; set; }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        [StringLengthValidator(AllowNulls = false, Max = int.MaxValue, MultiLine = true)]
        public string Script { get; set; }
    }

    [AutoInit]
    public static class DynamicSqlMigrationOperation
    {
        public static readonly ConstructSymbol<DynamicSqlMigrationEntity>.Simple Create;
        public static readonly ExecuteSymbol<DynamicSqlMigrationEntity> Save;
        public static readonly ExecuteSymbol<DynamicSqlMigrationEntity> Execute;
        public static readonly DeleteSymbol<DynamicSqlMigrationEntity> Delete;
    }

    public enum DynamicSqlMigrationMessage
    {
        TheMigrationIsAlreadyExecuted,
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class DynamicRenameEntity : Entity
    {
        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        [NotNullable, SqlDbType(Size = 200)]
        [StringLengthValidator(AllowNulls = false, Max = 200)]
        public string ReplacementKey { get; set; }

        [NotNullable, SqlDbType(Size = 200)]
        [StringLengthValidator(AllowNulls = false, Max = 200)]
        public string OldName { get; set; }

        [NotNullable, SqlDbType(Size = 200)]
        [StringLengthValidator(AllowNulls = false, Max = 200)]
        public string NewName { get; set; }

        static Expression<Func<DynamicRenameEntity, string>> ToStringExpression = @this => @this.ReplacementKey + ": " + @this.OldName + " -> " + @this.NewName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }


}
