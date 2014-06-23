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

namespace Signum.Entities.Excel
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class ExcelReportDN : IdentifiableEntity
    {
        QueryDN query;
        [NotNullValidator]
        public QueryDN Query
        {
            get { return query; }
            set { Set(ref query, value); }
        }
  
        [NotNullable]
        string displayName;
        [StringLengthValidator(Min = 3)]
        public string DisplayName
        {
            get { return displayName; }
            set { SetToStr(ref displayName, value); }
        }

        [NotNullable]
        EmbeddedFileDN file;
        [NotNullValidator]
        public EmbeddedFileDN File
        {
            get { return file; }
            set { Set(ref file, value); }
        }

        static readonly Expression<Func<ExcelReportDN, string>> ToStringExpression = e => e.displayName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class ExcelReportOperation
    {
        public static readonly ExecuteSymbol<ExcelReportDN> Save = OperationSymbol.Execute<ExcelReportDN>();
        public static readonly DeleteSymbol<ExcelReportDN> Delete = OperationSymbol.Delete<ExcelReportDN>();
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
