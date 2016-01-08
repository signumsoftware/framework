using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Signum.Entities.Scheduler;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class EmailReportEntity : Entity, ITaskEntity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotNullable]
        [NotNullValidator]
        public Lite<EmailTemplateEntity> EmailTemplate { get; set; }

        [ImplementedByAll]
        public Lite<Entity> Target { get; set; }

        static Expression<Func<EmailReportEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class EmailReportOperation
    {
        public static readonly ExecuteSymbol<EmailReportEntity> Save;
    }
}
