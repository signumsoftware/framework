using Signum.Utilities;
using System;
using System.Linq.Expressions;
using Signum.Entities.Scheduler;
using Signum.Entities.UserQueries;
using Signum.Entities.Templating;
using System.Reflection;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class SendEmailTaskEntity : Entity, ITaskEntity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Name { get; set; }

        public Lite<EmailTemplateEntity> EmailTemplate { get; set; }

        [ImplementedByAll]
        public Lite<Entity>? UniqueTarget { get; set; }

        public Lite<UserQueryEntity>? TargetsFromUserQuery { get; set; }

        public ModelConverterSymbol? ModelConverter { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);

        protected override string? PropertyValidation(PropertyInfo pi)
        {
            if(pi.Name == nameof(TargetsFromUserQuery) || pi.Name == nameof(UniqueTarget))
            {
                if (TargetsFromUserQuery == null && UniqueTarget == null)
                    return ValidationMessage._0Or1ShouldBeSet.NiceToString(
                        NicePropertyName(() => UniqueTarget),
                        NicePropertyName(() => TargetsFromUserQuery));

                if (UniqueTarget != null && TargetsFromUserQuery != null)
                    return ValidationMessage._0And1CanNotBeSetAtTheSameTime.NiceToString(
                        NicePropertyName(() => UniqueTarget),
                        NicePropertyName(() => TargetsFromUserQuery));
            }

            return base.PropertyValidation(pi);
        }
    }

    [AutoInit]
    public static class SendEmailTaskOperation
    {
        public static readonly ExecuteSymbol<SendEmailTaskEntity> Save;
    }
}
