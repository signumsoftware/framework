using Microsoft.Graph.Models;
using Npgsql.Replication.PgOutput.Messages;
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

    public static void Start(SchemaBuilder sb, IFileTypeAlgorithm algorithm)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
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
    }

    internal static async Task<CachedProfilePhotoEntity> GetOrCreateCachedPicture(Guid oid, int size)
    {
        using (AuthLogic.Disable())
        {
            var result = await Database.Query<CachedProfilePhotoEntity>().SingleOrDefaultAsync(a => a.User.Entity.Mixin<UserADMixin>().OID == oid && a.Size == size);

            if (result != null && result.InvalidationDate >= Clock.Today)
                return result;

            size = AzureADLogic.ToAzureSize(size);

            var stream = await AzureADLogic.GetUserPhoto(oid, size).ContinueWith(promise => promise.IsFaulted || promise.IsCanceled ? null : promise.Result);
            var newresult = new CachedProfilePhotoEntity();

            using (var tr = new Transaction())
            {
                result = await Database.Query<CachedProfilePhotoEntity>().SingleOrDefaultAsync(a => a.User.Entity.Mixin<UserADMixin>().OID == oid && a.Size == size);
                if (result != null && result.InvalidationDate >= Clock.Today)
                    return result;

                var user = Database.Query<UserEntity>().Where(u => u.Mixin<UserADMixin>().OID == oid).Select(a => a.ToLite()).SingleEx();

                if (result != null)
                    result.Delete();

                newresult = new CachedProfilePhotoEntity
                {
                    Photo = stream == null ? null : new FilePathEmbedded(AuthADFileType.CachedProfilePhoto, oid.ToString() + "x" + size + ".jpg", stream.ReadAllBytes()),
                    User = user,
                    InvalidationDate = stream == null ? Clock.Today.AddDays(7) : Clock.Today.AddMonths(1),
                    Size = size
                }.Save();

                tr.Commit();
            }

            return newresult;
        }
    }

    internal static Task<bool> HasCachedPicture(Guid oid, int size)
    {
        return Database.Query<CachedProfilePhotoEntity>().AnyAsync(a => a.User.Entity.Mixin<UserADMixin>().OID == oid && a.Size == size);
    }
}
