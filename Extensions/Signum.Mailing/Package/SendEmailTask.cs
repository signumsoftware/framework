using Signum.Entities.Validation;
using Signum.Mailing.Templates;
using Signum.Processes;
using Signum.Scheduler;
using Signum.Templating;
using Signum.UserQueries;

namespace Signum.Mailing.Package;

[EntityKind(EntityKind.Shared, EntityData.Master)]
public class SendEmailTaskEntity : Entity, ITaskEntity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    public Lite<EmailTemplateEntity> EmailTemplate { get; set; }

    public EmaiTemplateTargetFrom TargetFrom { get; set; }

    [ImplementedByAll]
    public Lite<Entity>? UniqueTarget { get; set; }

    public Lite<UserQueryEntity>? TargetsFromUserQuery { get; set; }

    public ModelConverterSymbol? ModelConverter { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if(pi.Name == nameof(UniqueTarget))
        {
            return (pi, UniqueTarget).IsSetOnlyWhen(TargetFrom == EmaiTemplateTargetFrom.Unique);
        }

        if (pi.Name == nameof(TargetsFromUserQuery))
        {
            return (pi, TargetsFromUserQuery).IsSetOnlyWhen(TargetFrom == EmaiTemplateTargetFrom.UserQuery);
        }

        return base.PropertyValidation(pi); 
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

   
}

public enum EmaiTemplateTargetFrom
{
    NoTarget,
    Unique,
    UserQuery,
}

[AutoInit]
public static class SendEmailTaskOperation
{
    public static readonly ExecuteSymbol<SendEmailTaskEntity> Save;
}


[AutoInit]
public static class EmailMessageProcess
{
    public static readonly ProcessAlgorithmSymbol CreateEmailsSendAsync;
    public static readonly ProcessAlgorithmSymbol SendEmails;
}

