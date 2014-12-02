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
    public class WordReportTemplateEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 200)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string Name
        {
            get { return name; }
            set { Set(ref name, value); }
        }

        [NotNullable]
        TypeEntity type;
        [NotNullValidator]
        public TypeEntity Type
        {
            get { return type; }
            set { Set(ref type, value); }
        }

        [NotNullable]
        QueryEntity query;
        [NotNullValidator]
        public QueryEntity Query
        {
            get { return query; }
            set { Set(ref query, value); }
        }

        SystemWordReportEntity systemWordReport;
        public SystemWordReportEntity SystemWordReport
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

        static Expression<Func<WordReportTemplateEntity, bool>> IsActiveNowExpression =
            (mt) => mt.active && TimeZoneManager.Now.IsInInterval(mt.StartDate, mt.EndDate);
        public bool IsActiveNow()
        {
            return IsActiveNowExpression.Evaluate(this);
        }

        [NotNullable]
        Lite<FileEntity> template;
        [NotNullValidator]
        public Lite<FileEntity> Template
        {
            get { return template; }
            set { Set(ref template, value); }
        }

        static Expression<Func<WordReportTemplateEntity, string>> ToStringExpression = e => e.Name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class WordReportTemplateOperation
    {
        public static readonly ExecuteSymbol<WordReportTemplateEntity> Save = OperationSymbol.Execute<WordReportTemplateEntity>();

        public static readonly ConstructSymbol<WordReportTemplateEntity>.From<SystemWordReportEntity> CreateWordReportTemplateFromSystemWordReport = OperationSymbol.Construct<WordReportTemplateEntity>.From<SystemWordReportEntity>();
    }

    public enum WordTemplateMessage
    {
        [Description("Model should be set to use model {0}")]
        ModelShouldBeSetToUseModel0,
        [Description("Type {0} does not have a property with name {1}")]
        Type0DoesNotHaveAPropertyWithName1,
    }
}
