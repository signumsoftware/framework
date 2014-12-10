using Signum.Entities.Basics;
using Signum.Entities.Files;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Word
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class WordTemplateEntity : Entity
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
        QueryEntity query;
        [NotNullValidator]
        public QueryEntity Query
        {
            get { return query; }
            set { Set(ref query, value); }
        }

        SystemWordTemplateEntity systemWordTemplate;
        public SystemWordTemplateEntity SystemWordTemplate
        {
            get { return systemWordTemplate; }
            set { Set(ref systemWordTemplate, value); }
        }

        [NotNullable]
        CultureInfoEntity culture;
        [NotNullValidator]
        public CultureInfoEntity Culture
        {
            get { return culture; }
            set { Set(ref culture, value); }
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

        bool disableAuthorization;
        public bool DisableAuthorization
        {
            get { return disableAuthorization; }
            set { Set(ref disableAuthorization, value); }
        }

        static Expression<Func<WordTemplateEntity, bool>> IsActiveNowExpression =
            (mt) => mt.active && TimeZoneManager.Now.IsInInterval(mt.StartDate, mt.EndDate);
        public bool IsActiveNow()
        {
            return IsActiveNowExpression.Evaluate(this);
        }

        Lite<FileEntity> template;
        public Lite<FileEntity> Template
        {
            get { return template; }
            set { Set(ref template, value); }
        }

        FileTypeSymbol fileType = WordReportFileType.DefaultWordReport;
        public FileTypeSymbol FileType
        {
            get { return fileType; }
            set { Set(ref fileType, value); }
        }

        static Expression<Func<WordTemplateEntity, string>> ToStringExpression = e => e.Name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => Template) && Template == null && Active)
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

            return base.PropertyValidation(pi);
        }
    }

    public static class WordTemplateOperation
    {
        public static readonly ExecuteSymbol<WordTemplateEntity> Save = OperationSymbol.Execute<WordTemplateEntity>();

        public static readonly ConstructSymbol<WordTemplateEntity>.From<SystemWordTemplateEntity> CreateWordTemplateFromSystemWordTemplate = OperationSymbol.Construct<WordTemplateEntity>.From<SystemWordTemplateEntity>();
    }

    public static class WordReportFileType
    {
        public static readonly FileTypeSymbol DefaultWordReport = new FileTypeSymbol(); 
        
    }

    public enum WordTemplateMessage
    {
        [Description("Model should be set to use model {0}")]
        ModelShouldBeSetToUseModel0,
        [Description("Type {0} does not have a property with name {1}")]
        Type0DoesNotHaveAPropertyWithName1,
    }

}
