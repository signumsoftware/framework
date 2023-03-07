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
    public ImportExcelModel() 
    {
        this.BindParent();
    }

    [StringLengthValidator(Max = 100)]
    public string TypeName { get; set; }

    public FileEmbedded ExcelFile { get; set; }

    [StringLengthValidator(Max = 100)]
    public string OperationKey { get; set; }

    public bool Transactional { get; set; }

    public bool IdentityInsert { get; set; }

    public ImportExcelMode Mode { get; set; }

    public string? MatchByColumn { get; set; }

    [NoRepeatValidator, BindParent]
    public MList<CollectionElementEmbedded> Collections { get; set; } = new MList<CollectionElementEmbedded>();

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(MatchByColumn))
            return (pi, MatchByColumn).IsSetOnlyWhen(Mode == ImportExcelMode.Update || Mode == ImportExcelMode.InsertOrUpdate || Mode == ImportExcelMode.Insert && Collections.Count > 0);

        return base.PropertyValidation(pi);
    }
}

public class CollectionElementEmbedded : EmbeddedEntity
{
    public string CollectionElement { get; set; }

    public string? MatchByColumn { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        var p = this.GetParentEntity<ImportExcelModel>();

        if (pi.Name == nameof(MatchByColumn))
            return (pi, MatchByColumn).IsSetOnlyWhen(p.Mode == ImportExcelMode.Update || p.Mode == ImportExcelMode.InsertOrUpdate || p.Mode == ImportExcelMode.Insert && this != p.Collections.Last());

        return base.PropertyValidation(pi);
    }
}

public enum ImportExcelMode
{
    Insert,
    Update,
    InsertOrUpdate,
}

