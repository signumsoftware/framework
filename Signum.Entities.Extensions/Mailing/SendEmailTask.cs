using Signum.Utilities;
using System;
using System.Linq.Expressions;
using Signum.Entities.Scheduler;
using Signum.Entities.UserQueries;
using Signum.Entities.Templating;

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
        public Lite<Entity> UniqueTarget { get; set; }

        public Lite<UserQueryEntity>? TargetsFromUserQuery { get; set; }

        public ModelConverterSymbol? ModelConverter { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);
    }

    [AutoInit]
    public static class SendEmailTaskOperation
    {
        public static readonly ExecuteSymbol<SendEmailTaskEntity> Save;
    }
}
