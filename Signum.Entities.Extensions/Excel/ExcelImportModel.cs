using Signum.Entities.Files;
using Signum.Entities.UserAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Excel;


public class ImportExcelModel : ModelEntity
{
    [StringLengthValidator(Max = 100)]
    public string TypeName { get; set; }

    public FileEmbedded ExcelFile { get; set; }

    [StringLengthValidator(Max = 100)]
    public string OperationKey { get; set; }

    public bool Transactional { get; set; }

    public bool IdentityInsert { get; set; }

    public ImportExcelMode Mode { get; set; }

    public string? MatchByColumn { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(MatchByColumn) && MatchByColumn == null && (Mode == ImportExcelMode.InsertOrUpdate || Mode == ImportExcelMode.Update))
            return ValidationMessage._0IsMandatoryWhen1IsSetTo2.NiceToString(pi.NiceName(), NicePropertyName(() => Mode), Mode.NiceToString());

        return base.PropertyValidation(pi);
    }
}

public enum ImportExcelMode
{
    Insert,
    Update,
    InsertOrUpdate,
}

