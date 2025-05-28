using Signum.Authorization;
using System.ComponentModel;

namespace Signum.Dynamic.SqlMigrations;

[EntityKind(EntityKind.Main, EntityData.Transactional)]
public class DynamicSqlMigrationEntity : Entity
{
    public DateTime CreationDate { get; set; }

    [ImplementedBy(typeof(UserEntity))]

    public Lite<IUserEntity> CreatedBy { get; set; }

    public DateTime? ExecutionDate { get; set; }

    [ImplementedBy(typeof(UserEntity))]
    public Lite<IUserEntity>? ExecutedBy { get; set; }

    [StringLengthValidator(Min = 3, Max = 200)]
    public string Comment { get; set; }

    [StringLengthValidator(Max = int.MaxValue, MultiLine = true)]
    public string Script { get; set; }


    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Comment.Etc(100));
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

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class DynamicRenameEntity : Entity
{
    public DateTime CreationDate { get; private set; } = Clock.Now;

    [StringLengthValidator(Max = 200)]
    public string ReplacementKey { get; set; }

    [StringLengthValidator(Max = 200)]
    public string OldName { get; set; }

    [StringLengthValidator(Max = 200)]
    public string NewName { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => ReplacementKey + ": " + OldName + " -> " + NewName);
}


