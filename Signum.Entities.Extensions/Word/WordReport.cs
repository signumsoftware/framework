using Signum.Entities.Basics;
using Signum.Entities.Files;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Word
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class WordReportTemplateDN : Entity
    {
        [NotNullable]
        TypeDN type;
        [NotNullValidator]
        public TypeDN Type
        {
            get { return type; }
            set { Set(ref type, value); }
        }

        [NotNullable]
        QueryDN query;
        [NotNullValidator]
        public QueryDN Query
        {
            get { return query; }
            set { Set(ref query, value); }
        }

        SystemWordReportDN systemWordReport;
        public SystemWordReportDN SystemWordReport
        {
            get { return systemWordReport; }
            set { Set(ref systemWordReport, value); }
        }

        bool active;
        public bool Active
        {
            get { return active; }
            set { Set(ref active, value); }
        }

        DateTime startDate = TimeZoneManager.Now.TrimToMinutes();
        [MinutesPrecissionValidator]
        public DateTime StartDate
        {
            get { return startDate; }
            set { Set(ref startDate, value); }
        }

        DateTime? endDate;
        [MinutesPrecissionValidator]
        public DateTime? EndDate
        {
            get { return endDate; }
            set { Set(ref endDate, value); }
        }

        static Expression<Func<WordReportTemplateDN, bool>> IsActiveNowExpression =
            (mt) => mt.active && TimeZoneManager.Now.IsInInterval(mt.StartDate, mt.EndDate);
        public bool IsActiveNow()
        {
            return IsActiveNowExpression.Evaluate(this);
        }

        [NotNullable]
        Lite<FileDN> template;
        [NotNullValidator]
        public Lite<FileDN> Template
        {
            get { return template; }
            set { Set(ref template, value); }
        }
    }

    public static class WordReportTemplateOperation
    {
        public static readonly ExecuteSymbol<WordReportTemplateDN> Save = OperationSymbol.Execute<WordReportTemplateDN>();

        public static readonly ConstructSymbol<WordReportTemplateDN>.From<SystemWordReportDN> CreateWordReportTemplateFromSystemWordReport = OperationSymbol.Construct<WordReportTemplateDN>.From<SystemWordReportDN>();
    }

    public enum WordTemplateMessage
    {
        [Description("Model should be set to use model {0}")]
        ModelShouldBeSetToUseModel0,
        [Description("Type {0} does not have a property with name {1}")]
        Type0DoesNotHaveAPropertyWithName1,
    }
}
