using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Word
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class WordReportLogEntity : Entity
    {
        [ImplementedByAll]
        Lite<Entity> target;
        public Lite<Entity> Target
        {
            get { return target; }
            set { Set(ref target, value); }
        }

        Lite<IUserEntity> user;
        [NotNullValidator]
        public Lite<IUserEntity> User
        {
            get { return user; }
            set { SetToStr(ref user, value); }
        }

        Lite<WordTemplateEntity> template;
        public Lite<WordTemplateEntity> Template
        {
            get { return template; }
            set { Set(ref template, value); }
        }

        DateTime start;
        public DateTime Start
        {
            get { return start; }
            set { SetToStr(ref start, value); }
        }

        DateTime? end;
        public DateTime? End
        {
            get { return end; }
            set { Set(ref end, value); }
        }

        static Expression<Func<WordReportLogEntity, double?>> DurationExpression =
            log => (double?)(log.End - log.Start).Value.TotalMilliseconds;
        public double? Duration
        {
            get { return end == null ? null : DurationExpression.Evaluate(this); }
        }

        Lite<ExceptionEntity> exception;
        public Lite<ExceptionEntity> Exception
        {
            get { return exception; }
            set { Set(ref exception, value); }
        }

        public override string ToString()
        {
            return "{0} {1} {2:d}".FormatWith(template, user, start);
        }
    }

    public static class WordReportLogOperation
    {
        public static readonly ConstructSymbol<WordReportLogEntity>.From<WordTemplateEntity> CreateWordReportFromTemplate = OperationSymbol.Construct<WordReportLogEntity>.From<WordTemplateEntity>();
        public static readonly ConstructSymbol<WordReportLogEntity>.From<Entity> CreateWordReportFromEntity = OperationSymbol.Construct<WordReportLogEntity>.From<Entity>();
    }
}
