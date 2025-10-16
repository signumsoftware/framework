using Microsoft.AspNetCore.Mvc;
using System.IO;
using Signum.Utilities.Reflection;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Reflection.Metadata.Ecma335;
using Azure.Core;

namespace Signum.Files;

public class FilesController : ControllerBase
{
    [HttpGet("api/files/downloadFile/{fileId}")]
    public FileStreamResult DownloadFile(string fileId)
    {
        var file = Database.Retrieve<FileEntity>(PrimaryKey.Parse(fileId, typeof(FileEntity)));

        return MimeMapping.GetFileStreamResult(new MemoryStream(file.BinaryFile), file.FileName);

    }

    [HttpGet("api/files/downloadFilePath/{filePathId}")]
    public FileStreamResult DownloadFilePath(string filePathId)
    {
        var filePath = Database.Retrieve<FilePathEntity>(PrimaryKey.Parse(filePathId, typeof(FilePathEntity)));

   		Response.Headers.CacheControl = $"max-age={FilePathLogic.MaxAge(filePath)}, private";

        return MimeMapping.GetFileStreamResult(filePath.OpenRead(), filePath.FileName);
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

    	Response.Headers.CacheControl = $"max-age={FilePathLogic.MaxAge(fpe)}, private";

        return MimeMapping.GetFileStreamResult(fpe.OpenRead(), fpe.FileName);
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

    [HttpPost("api/files/startUpload")]
    public async Task<string> StartUpload([FromBody] StartUploadRequest request)
    {
        var fileType = SymbolLogic<FileTypeSymbol>.ToSymbol(request.FileTypeKey);

        var algorithm = FileTypeLogic.GetAlgorithm(fileType);

        IFilePath fpe = CreateEmptyFile(request.Type, fileType, request.FileName, null);

        await algorithm.StartUpload(fpe);

        return fpe.Suffix;
    }

    public class StartUploadRequest
    {
        public string FileTypeKey { get; set; }
        public string FileName { get; set; }
        public string Type { get; set; }
    }

    [HttpPost("api/files/uploadChunk")]
    public async Task<ChunkInfo> UploadChunk(
        [FromQuery] string fileName,
        [FromQuery] string fileTypeKey,
        [FromQuery] string suffix,
        [FromQuery] string type,
        [FromQuery] int chunkIndex,
        CancellationToken token)
    {
        var fileType = SymbolLogic<FileTypeSymbol>.ToSymbol(fileTypeKey);

        var algorithm = FileTypeLogic.GetAlgorithm(fileType);

        IFilePath fpe = CreateEmptyFile(type, fileType, fileName, suffix);

        using MemoryStream ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, token);
        ms.Position = 0;

        var chunkInfo = await algorithm.UploadChunk(fpe, chunkIndex, ms, token);

        return chunkInfo;
    }


    static IFilePath CreateEmptyFile(string type, FileTypeSymbol fileType, string fileName, string? suffix)
    {
        if (type == typeof(FilePathEmbedded).Name)
            return new FilePathEmbedded(fileType, fileName, null!) { Suffix = suffix! };

        if (type == TypeLogic.TryGetCleanName(typeof(FilePathEntity)))
            return new FilePathEntity(fileType, fileName, null!) { Suffix = suffix! };

        throw new UnexpectedValueException(type);
    }

    [HttpPost("api/files/finishUpload")]
    public async Task<FinishUploadResponse> FinishUpload([FromBody] FinishUploadRequest request, CancellationToken token)
    {
        var fileType = SymbolLogic<FileTypeSymbol>.ToSymbol(request.FileTypeKey);

        var algorithm = FileTypeLogic.GetAlgorithm(fileType);

        IFilePath fpe = CreateEmptyFile(request.Type, fileType, request.FileName, request.Suffix);

        await algorithm.FinishUpload(fpe, request.Chunks, token);

        return new FinishUploadResponse { Hash = fpe.Hash!, FileLength = fpe.FileLength, FullWebPath = fpe.FullWebPath() };
    }


    public class FinishUploadRequest
    {
        public string FileTypeKey { get; set; }
        public string FileName { get; set; }
        public string Suffix { get; set; }
        public string Type { get; set; }
        public List<ChunkInfo> Chunks { get; set; }
    }

    public class FinishUploadResponse
    {
        public long FileLength { get; set; }
        public string Hash { get; set; }
        public string? FullWebPath { get; internal set; }
    }
}
