using Microsoft.Graph.Models;
using Npgsql.Replication.PgOutput.Messages;
using Signum.Files;
using System.IO;

namespace Signum.Authorization.AzureAD;

public static class CachedProfilePhotoLogic
{
    public static bool IsStarted = false;
    public static int DefaultSize = AzureADLogic.ToAzureSize(22);


    [AutoExpressionField]
    public static IQueryable<CachedProfilePhotoEntity> CachedProfilePhotos(this UserEntity u) =>
        As.Expression(() => Database.Query<CachedProfilePhotoEntity>().Where(a => a.User.Is(u)));

    public static void Start(SchemaBuilder sb, IFileTypeAlgorithm algorithm)
    {
        if (sb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        IsStarted = true;

        sb.Include<CachedProfilePhotoEntity>()
            .WithDelete(CachedProfilePhotoOperation.Delete)
            .WithExpressionFrom((UserEntity u) => u.CachedProfilePhotos())
            .WithUniqueIndex(u => new { u.User, u.Size })
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.CreationDate,
                e.InvalidationDate,
                e.Size,
                e.User,
                e.Photo,
            });

        FileTypeLogic.Register(AuthADFileType.CachedProfilePhoto, algorithm);
    }

    public static Func<CachedProfilePhotoEntity, DateTime> CalculateInvalidationDate = p => p.Photo == null ? Clock.Now.AddDays(7) : Clock.Now.AddMonths(1);

    internal static async Task<CachedProfilePhotoEntity> GetOrCreateCachedPicture(Guid oid, int size)
    {
        using (AuthLogic.Disable())
        {
            size = AzureADLogic.ToAzureSize(size);

            var result = await Database.Query<CachedProfilePhotoEntity>().SingleOrDefaultAsync(a => a.User.Entity.Mixin<UserAzureADMixin>().OID == oid && a.Size == size);

            if (result != null && result.InvalidationDate >= Clock.Now)
                return result;

            var stream = await AzureADLogic.GetUserPhoto(oid, size).ContinueWith(promise => promise.IsFaulted || promise.IsCanceled ? null : promise.Result);

            using (var tr = new Transaction())
            {
                result = await Database.Query<CachedProfilePhotoEntity>().SingleOrDefaultAsync(a => a.User.Entity.Mixin<UserAzureADMixin>().OID == oid && a.Size == size);
                if (result != null && result.InvalidationDate >= Clock.Now)
                    return tr.Commit(result);

                var bytes = stream?.ReadAllBytes();

                if (result != null)
                {
                    if (bytes == null && result.Photo == null ||
                       bytes != null && result.Photo != null && bytes.AsSpan().SequenceEqual(result.Photo.GetByteArray().AsSpan()))
                    {
                        result.InvalidationDate = CalculateInvalidationDate(result);
                        result.Save();
                        return tr.Commit(result);
                    }
                    else
                    {
                        result.Photo = bytes == null ? null : new FilePathEmbedded(AuthADFileType.CachedProfilePhoto, oid.ToString() + "x" + size + ".jpg", bytes);
                        result.InvalidationDate = CalculateInvalidationDate(result);
                        result.Save();
                        return tr.Commit(result);
                    }
                }
                else
                {
                    var user = Database.Query<UserEntity>().Where(u => u.Mixin<UserAzureADMixin>().OID == oid).Select(a => a.ToLite()).SingleEx();
                    result = new CachedProfilePhotoEntity
                    {
                        Photo = bytes == null ? null : new FilePathEmbedded(AuthADFileType.CachedProfilePhoto, oid.ToString() + "x" + size + ".jpg", bytes),
                        User = user,
                        Size = size,
                    };

                    result.InvalidationDate = CalculateInvalidationDate(result);

                    result.Save();

                    return tr.Commit(result);
                }
            }
        }
    }

    internal static Task<bool> HasCachedPicture(Guid oid, int size)
    {
        return Database.Query<CachedProfilePhotoEntity>().AnyAsync(a => a.User.Entity.Mixin<UserAzureADMixin>().OID == oid && a.Size == size);
    }
}
