using System;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Entities.Files;
using System.Linq.Expressions;
using System.ComponentModel;

namespace Signum.Entities.Excel
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class ExcelReportEntity : Entity
    {
        
        public QueryEntity Query { get; set; }

        [StringLengthValidator(Min = 3, Max = 200)]
        public string DisplayName { get; set; }

        
        public FileEmbedded File { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => DisplayName);
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
