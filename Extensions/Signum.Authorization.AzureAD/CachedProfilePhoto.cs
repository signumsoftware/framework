using Signum.Files;
using System.ComponentModel;

namespace Signum.Authorization.AzureAD;

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class CachedProfilePhotoEntity : Entity
{
    public Lite<UserEntity> User { get; set; }

    [Unit("px"), NumberIsValidator(ComparisonType.GreaterThan, 0)]
    public int Size { get; set; }

    [DefaultFileType(nameof(AuthADFileType.CachedProfilePhoto))]
    public FilePathEmbedded? Photo { get; set; }

    public DateTime InvalidationDate { get; set; }

    public DateTime CreationDate { get; private set; } = Clock.Now;

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Size) && Size != AzureADLogic.ToAzureSize(Size))
            return ValidationMessage._0ShouldBe1.NiceToString(pi.NiceName(), AzureADLogic.ToAzureSize(Size));

        return base.PropertyValidation(pi);
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{User} {Size}px");
}

[AutoInit]
public static class CachedProfilePhotoOperation
{
    public static readonly ExecuteSymbol<CachedProfilePhotoEntity> Save;
    public static readonly DeleteSymbol<CachedProfilePhotoEntity> Delete;
}

[AutoInit]
public static class AuthADFileType
{
    public static readonly FileTypeSymbol CachedProfilePhoto;
}
