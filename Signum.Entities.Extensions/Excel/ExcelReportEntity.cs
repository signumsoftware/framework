using Signum.Entities.Basics;
using Signum.Entities.Files;
using System.ComponentModel;
using Signum.Entities.Authorization;

namespace Signum.Entities.Excel;

[EntityKind(EntityKind.Main, EntityData.Master)]
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
    CreateNew,
}

public enum ImportFromExcelMessage
{
    [Description("Import from Excel")]
    ImportFromExcel,
    [Description("{0} errors")]
    _0Errors,
    [Description("Importing {0}")]
    Importing0,
    [Description("Import {0} from Excel")]
    Import0FromExcel,
    [Description("Download Excel Template for this Query")]
    DownloadTemplate,
    [Description("Column(s) {0} already have constant values from filters")]
    Columns0AlreadyHaveConstanValuesFromFilters,

    [Description("This query has multiple implementations {0}")]
    ThisQueryHasMultipleImplementations0,


    [Description("Some Columns are incompatible with importing from Excel:")]
    SomeColumnsAreIncompatibleWithImportingFromExcel,

    [Description("Operation {0} is no supported")]
    Operation0IsNotSupported,

    [Description("Many filters try to assign the same property {0} with different values {1}")]
    ManyFiltersTryToAssignTheSameProperty0WithDifferentValues1,

    [Description("{0} is not supported")]
    _0IsNotSupported,


    [Description("{0} ({1}) can not be assigned directly. Each nested field should be assigned independently.")]
    _01CanNotBeAssignedDirectylEachNestedFieldShouldBeAssignedIndependently,

    [Description("{0}.[{1}] can also be used")]
    _01CanAlsoBeUsed,

    [Description("{0} is read-only")]
    _0IsReadOnly,
 
    [Description("{0} ({1}) is incompatible")]
    _01IsIncompatible,

    [Description("Errors in {0} row[s]")]
    ErrorsIn0Rows_N,

    [Description("No {0} found in this query with {1} equals to {2}")]
    No0FoundInThisQueryWith1EqualsTo2,

    [Description("Unable to assign more than one unrelated collection: {0}")]
    UnableToAssignMoreThanOneUnrelatedCollections0,

    [Description("Duplicate non-consecutive {0} found: {1}")]
    DuplicatedNonConsecutive0Found1,

    [Description("Columns do not match.\nExcel columns: {0}\nQuery columns: {1}")]
    ColumnsDoNotMatchExcelColumns0QueryColumns1
}


[AutoInit]
public static class ExcelPermission
{
    public static PermissionSymbol PlainExcel;
    public static PermissionSymbol ImportFromExcel;
}
