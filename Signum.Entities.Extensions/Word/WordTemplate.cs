using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Files;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Word
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class WordTemplateEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 200)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string Name { get; set; }

        [NotNullable]
        [NotNullValidator]
        public QueryEntity Query { get; set; }

        public SystemWordTemplateEntity SystemWordTemplate { get; set; }

        [NotNullable]
        [NotNullValidator]
        public CultureInfoEntity Culture { get; set; }

        public bool Active { get; set; }

        [MinutesPrecissionValidator]
        public DateTime? StartDate { get; set; }

        [MinutesPrecissionValidator]
        public DateTime? EndDate { get; set; }

        public bool DisableAuthorization { get; set; }

        static Expression<Func<WordTemplateEntity, bool>> IsActiveNowExpression =
            (mt) => mt.Active && TimeZoneManager.Now.IsInInterval(mt.StartDate, mt.EndDate);
        [ExpressionField]
        public bool IsActiveNow()
        {
            return IsActiveNowExpression.Evaluate(this);
        }

        public Lite<FileEntity> Template { get; set; }

        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100), FileNameValidator]
        public string FileName { get; set; }

        public WordTransformerSymbol WordTransformer { get; set; }

        public WordConverterSymbol WordConverter { get; set; }

        static Expression<Func<WordTemplateEntity, string>> ToStringExpression = e => e.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Template) && Template == null && Active)
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

            return base.PropertyValidation(pi);
        }
    }

    [AutoInit]
    public static class WordTemplateOperation
    {
        public static ExecuteSymbol<WordTemplateEntity> Save;
        public static ExecuteSymbol<WordTemplateEntity> CreateWordReport;

        public static ConstructSymbol<WordTemplateEntity>.From<SystemWordTemplateEntity> CreateWordTemplateFromSystemWordTemplate;
    }

    public enum WordTemplateMessage
    {
        [Description("Model should be set to use model {0}")]
        ModelShouldBeSetToUseModel0,
        [Description("Type {0} does not have a property with name {1}")]
        Type0DoesNotHaveAPropertyWithName1,
        ChooseAReportTemplate,
        [Description("{0} {1} requires extra parameters")]
        _01RequiresExtraParameters,
    }

    [Serializable]
    public class WordTransformerSymbol : Symbol
    {
        private WordTransformerSymbol() { }

        public WordTransformerSymbol(Type declaringType, string fieldName) :
            base(declaringType, fieldName)
        {
        }
    }

    [Serializable]
    public class WordConverterSymbol : Symbol
    {
        private WordConverterSymbol() { }

        public WordConverterSymbol(Type declaringType, string fieldName) :
            base(declaringType, fieldName)
        {
        }
    }

    [AutoInit]
    public static class WordTemplatePermission
    {
        public static PermissionSymbol GenerateReport;
    }
}
