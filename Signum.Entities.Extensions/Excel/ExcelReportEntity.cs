using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Entities.Files;
using System.Linq.Expressions;
using System.ComponentModel;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Excel
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class ExcelReportEntity : Entity
    {
        [NotNullValidator]
        public QueryEntity Query { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
        public string DisplayName { get; set; }

        [NotNullValidator]
        public FileEmbedded File { get; set; }

        static readonly Expression<Func<ExcelReportEntity, string>> ToStringExpression = e => e.DisplayName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class ExcelReportOperation
    {
        public static ExecuteSymbol<ExcelReportEntity> Save;
        public static DeleteSymbol<ExcelReportEntity> Delete;
    }

    public enum ExcelMessage
    {
        Data,
        Download,
        [Description("Microsoft Office Excel 2007 Spreadsheet (*.xlsx)|*.xlsx")]
        Excel2007Spreadsheet,
        [Description("Administer")]
        Administer,
        [Description("Excel Report")]
        ExcelReport,
        [Description("Excel template must have .xlsx extension, and the current one has {0}")]
        ExcelTemplateMustHaveExtensionXLSXandCurrentOneHas0,
        [Description("Find location for Excel Report")]
        FindLocationFoExcelReport,
        Reports,
        [Description("The Excel Template has a column {0} not present in the Find Window")]
        TheExcelTemplateHasAColumn0NotPresentInTheFindWindow,
        ThereAreNoResultsToWrite,
        CreateNew
    }

}
