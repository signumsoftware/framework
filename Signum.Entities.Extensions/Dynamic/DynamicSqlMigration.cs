using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        [ImplementedBy(typeof(UserEntity))]
        [NotNullValidator]
        public Lite<IUserEntity> CreatedBy { get; set; }

        public DateTime? ExecutionDate { get; set; }
        
        [ImplementedBy(typeof(UserEntity))]
        public Lite<IUserEntity> ExecutedBy { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string Comment { get; set; }

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
        [Description("Preventing the generation of a new Script because of errors in dynamic code. Fix the errors and restart the server.")]
        PreventingGenerationNewScriptBecauseOfErrorsInDynamicCodeFixErrorsAndRestartServer,
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class DynamicRenameEntity : Entity
    {
        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        [StringLengthValidator(AllowNulls = false, Max = 200)]
        public string ReplacementKey { get; set; }

        [StringLengthValidator(AllowNulls = false, Max = 200)]
        public string OldName { get; set; }

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
