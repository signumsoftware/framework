using Signum.Authorization.ActiveDirectory.Azure;
using Signum.Files;
using System.IO;

namespace Signum.Authorization.ActiveDirectory;

public static class CachedProfilePhotoLogic
{
    public static bool IsStarted = false;
    public static int DefaultSize = AzureADLogic.ToAzureSize(22);


    [AutoExpressionField]
    public static IQueryable<CachedProfilePhotoEntity> CachedProfilePhotos(this UserEntity u) =>
        As.Expression(() => Database.Query<CachedProfilePhotoEntity>().Where(a => a.User.Is(u)));

    [AutoExpressionField]
    public static string? DefaultCachedProfilePhotoSuffix(this UserEntity u) =>
        As.Expression(() => Database.Query<CachedProfilePhotoEntity>().Where(a => a.User.Is(u) && a.Size == DefaultSize).Select(a => a.Photo.Suffix).SingleOrDefaultEx());

    public static void Start(SchemaBuilder sb, IFileTypeAlgorithm algorithm)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            IsStarted = true;

            sb.Include<CachedProfilePhotoEntity>()
                .WithDelete(CachedProfilePhotoOperation.Delete)
                
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.User,
                    e.Size,
                });

            FileTypeLogic.Register(AuthADFileType.CachedProfilePhoto, algorithm);
        }
    }

    internal static async CachedProfilePhotoEntity? GetCachedPicture(Guid oid, int size)
    {
        return Database.Query<CachedProfilePhotoEntity>().SingleOrDefaultAsync(a => a.User.Entity.Mixin<UserADMixin>().OID == oid && a.Size == size);
    }
}
