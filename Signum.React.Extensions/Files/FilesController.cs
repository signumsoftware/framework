using Microsoft.AspNetCore.Mvc;
using Signum.Entities.Files;
using Signum.Engine.Files;
using System.IO;
using Signum.Engine.Mailing;
using Signum.Entities.Basics;
using Signum.Utilities.Reflection;
using System.Collections.Concurrent;

namespace Signum.React.Files;

public class FilesController : ControllerBase
{
    [HttpGet("api/files/downloadFile/{fileId}")]
    public FileStreamResult DownloadFile(string fileId)
    {
        var file = Database.Retrieve<FileEntity>(PrimaryKey.Parse(fileId, typeof(FileEntity)));

        return GetFileStreamResult(new MemoryStream(file.BinaryFile), file.FileName);

    }

    [HttpGet("api/files/downloadFilePath/{filePathId}")]
    public FileStreamResult DownloadFilePath(string filePathId)
    {
        var filePath = Database.Retrieve<FilePathEntity>(PrimaryKey.Parse(filePathId, typeof(FilePathEntity)));

        return GetFileStreamResult(filePath.OpenRead(), filePath.FileName);
    }

    [HttpGet("api/files/downloadEmbeddedFilePath/{rootType}/{id}")]
    public FileStreamResult? DownloadFilePathEmbedded(string rootType, string id, string route, string? rowId)
    {
        var type = TypeLogic.GetType(rootType);

        var propertyRoute = PropertyRoute.Parse(type, route);
        var primaryKey = PrimaryKey.Parse(id, type);

        var makeQuery = queryBuilderCache.GetOrAdd(propertyRoute, pr =>
        {
            if (propertyRoute.Type != typeof(FilePathEmbedded))
                throw new InvalidOperationException($"Route {route} doesn't point to a FilePathEmbedded");

            var mlistRoute = propertyRoute.GetMListItemsRoute()!;
            if (mlistRoute == null)
                return giGetSimpleQuery.GetInvoker(type)(propertyRoute);
            else
                return giGetMListQuery.GetInvoker(type, mlistRoute.Type)(propertyRoute, mlistRoute);
        });

        var fpe = makeQuery(primaryKey, rowId);
        if (fpe == null)
            return null;

        return GetFileStreamResult(fpe.OpenRead(), fpe.FileName);
    }

    static ConcurrentDictionary<PropertyRoute, Func<PrimaryKey, string?, FilePathEmbedded?>> queryBuilderCache = 
        new ConcurrentDictionary<PropertyRoute, Func<PrimaryKey, string?, FilePathEmbedded?>>();

    static GenericInvoker<Func<PropertyRoute, Func<PrimaryKey, string?, FilePathEmbedded?>>> giGetSimpleQuery =
        new((propertyRoute) => GetSimpleQuery<Entity>(propertyRoute));
    static Func<PrimaryKey, string?, FilePathEmbedded?> GetSimpleQuery<T>(PropertyRoute propertyRoute)
        where T : Entity
    {
        var selector = propertyRoute.GetLambdaExpression<T, FilePathEmbedded?>(false);

        return (pk, rowId) =>
        {
            return Database.Query<T>()
            .Where(a => a.Id == pk)
            .Select(selector)
            .SingleEx();
        };
    }

    static GenericInvoker<Func<PropertyRoute, PropertyRoute, Func<PrimaryKey, string?, FilePathEmbedded?>>> giGetMListQuery =
       new((route, mlistRoute) => GetMListQuery<Entity, EmbeddedEntity>(route, mlistRoute));
    static Func<PrimaryKey, string?, FilePathEmbedded?> GetMListQuery<T, M>(PropertyRoute route, PropertyRoute mlistRoute)
        where T : Entity
    {
        var mlistExpression = mlistRoute.Parent!.GetLambdaExpression<T, MList<M>>(false);
        var elementExpression = route.GetLambdaExpression<M, FilePathEmbedded>(false, mlistRoute);
        var mlistPkType = Schema.Current.TableMList(mlistExpression).PrimaryKey.Type;

        return (pk, rowId) => {
            var rowIdPk = new PrimaryKey((IComparable)ReflectionTools.ChangeType(rowId, mlistPkType)!);

            return Database.MListQuery(mlistExpression)
            .Where(mle => mle.Parent.Id == pk && mle.RowId == rowIdPk)
            .Select(mle => elementExpression.Evaluate(mle.Element))
            .SingleEx();
        };
    }

    /// <param name="stream">No need to close</param
    public static FileStreamResult GetFileStreamResult(Stream stream, string fileName, bool forDownload = true)
    {
        var mime = MimeMapping.GetMimeType(fileName);
        return new FileStreamResult(stream, mime)
        {
            FileDownloadName = forDownload ? Path.GetFileName(fileName) : null
        };
    }

    public static FileStreamResult GetFileStreamResult(FileContent file, bool forDownload = true)
    {
        return GetFileStreamResult(new MemoryStream(file.Bytes), file.FileName, forDownload);
    }
}
