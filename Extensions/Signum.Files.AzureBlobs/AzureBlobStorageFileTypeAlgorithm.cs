using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Signum.Utilities.Synchronization;
using System.IO;

namespace Signum.Files.AzureBlobs;

public enum BlobAction
{
    Open,
    Download
}

public enum AzureWebDownload
{
    DirectUrl,
    SASToken,
    None
}

public class AzureBlobStorageFileTypeAlgorithm : FileTypeAlgorithmBase, IFileTypeAlgorithm
{
    public Func<IFilePath, BlobContainerClient> GetClient { get; private set; }

    public Func<AzureWebDownload> WebDownload { get; set; } = () => AzureWebDownload.None;

    public Func<IFilePath, string> CalculateSuffix { get; set; } = SuffixGenerators.Safe.YearMonth_Guid_Filename;
    public bool WeakFileReference { get; set; }
    public bool CreateBlobContainerIfNotExists { get; set; }

    //ExistBlob is too slow, consider using CalculateSuffix with a GUID!
    public Func<string, int, string>? RenameAlgorithm { get; set; } = null; // FileTypeAlgorithm.DefaultRenameAlgorithm;

    public Func<IFilePath, BlobAction> GetBlobAction { get; set; } = (IFilePath ifp) => BlobAction.Download;

    public Func<IFilePath, AzureDefenderPollingOptions?>? AzureDefenderPolling;

    public AzureBlobStorageFileTypeAlgorithm(Func<IFilePath, BlobContainerClient> getClient, bool directDownload = false)
    {
        this.GetClient = getClient;
    }

    public override bool OnlyImages
    {
        get { return base.OnlyImages; }
        set
        {
            base.OnlyImages = value;
            if (value)
                GetBlobAction = fp => BlobAction.Open;
        }
    }

    public Func<IFilePath, TimeSpan> SASTokenExpires = (IFilePath efp) => TimeSpan.FromMinutes(15);

    public string? GetFullPhysicalPath(IFilePath efp) => null;

    public string? GetFullWebPath(IFilePath efp)
    {
        var download = this.WebDownload();
        if (download == AzureWebDownload.None)
            return null;

        var client = GetClient(efp);
        if (download == AzureWebDownload.SASToken)
        {
            using (HeavyProfiler.LogNoStackTrace("Create SAS Token"))
            {
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = client.Name,
                    BlobName = efp.Suffix,
                    Resource = "b", // "b" = blob, "c" = container
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                    ExpiresOn = DateTimeOffset.UtcNow.Add(SASTokenExpires(efp))
                };

                // Define permissions (Read, Write, Delete, etc.)
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                // Generate the SAS token using the storage account key

                var sasToken = sasBuilder.ToSasQueryParameters(GetCredentials(client)).ToString();

                return $"{client.Uri}/{efp.Suffix}?{sasToken}";
            }
        }

        if (download == AzureWebDownload.DirectUrl)
            return $"{client.Uri}/{efp.Suffix}";

        throw new UnexpectedValueException(download);
    }

    static readonly Func<BlobContainerClient, StorageSharedKeyCredential> GetCredentials;

    static AzureBlobStorageFileTypeAlgorithm()
    {
        var param = Expression.Parameter(typeof(BlobContainerClient));

        var body = Expression.Property(Expression.Property(param, "ClientConfiguration"), "SharedKeyCredential");

        GetCredentials = Expression.Lambda<Func<BlobContainerClient, StorageSharedKeyCredential>>(body, param).Compile();
    }

    public BlobProperties GetProperties(IFilePath fp)
    {
        using (HeavyProfiler.Log("AzureBlobStorage GetProperties", () => fp.Suffix))
        {
            var client = GetClient(fp);
            return client.GetBlobClient(fp.Suffix).GetProperties();
        }
    }


    public Stream OpenRead(IFilePath fp)
    {
        using (HeavyProfiler.Log("AzureBlobStorage OpenRead", () => fp.Suffix))
        {
            var client = GetClient(fp);
            return client.GetBlobClient(fp.Suffix).Download().Value.Content;
        }
    }

    public byte[] ReadAllBytes(IFilePath fp)
    {
        using (HeavyProfiler.Log("AzureBlobStorage ReadAllBytes", () => fp.Suffix))
        {
            try
            {
                var client = GetClient(fp);
                return client.GetBlobClient(fp.Suffix).Download().Value.Content.ReadAllBytes();
            }
            catch (Exception ex)
            {
                ex.Data["suffix"] = fp.Suffix;
                throw;
            }
        }

    }

    public string GetAsString(BlobClient blobClient)
    {
        BlobDownloadResult downloadResult = blobClient.DownloadContentAsync().Result;
        string content = downloadResult.Content.ToString();

        return content;
    }

    public virtual void SaveFile(IFilePath fp)
    {
        using (HeavyProfiler.Log("AzureBlobStorage SaveFile"))
        using (new EntityCache(EntityCacheType.ForceNew))
        {
            if (WeakFileReference)
                return;

            BlobContainerClient client = CalculateSuffixWithRenames(fp);

            try
            {
                var blobHeaders = GetBlobHttpHeaders(fp, this.GetBlobAction(fp));
                var blobClient = client.GetBlobClient(fp.Suffix);
                SaveFileInAzure(blobClient, blobHeaders, fp.BinaryFile, this.AzureDefenderPolling?.Invoke(fp));
                fp.CleanBinaryFile();
            }
            catch (Exception ex)
            {
                ex.Data.Add("Suffix", fp.Suffix);
                ex.Data.Add("AccountName", client.AccountName);
                ex.Data.Add("ContainerName", client.Name);
                throw;
            }
        }
    }

    static void SaveFileInAzure(BlobClient blobClient, BlobHttpHeaders blobHeaders, byte[] binaryFile, AzureDefenderPollingOptions? azureDefenderPollingOptions)
    {
        blobClient.Upload(new MemoryStream(binaryFile), httpHeaders: blobHeaders);
        if (azureDefenderPollingOptions != null)
        {
            CheckBlobStorageFileForWindowsDefenderLogs(blobClient, azureDefenderPollingOptions, CancellationToken.None).WaitSafe();
        }
    }

    //Initial exceptions (like connection string problems) should happen synchronously
    public virtual /*async*/ Task SaveFileAsync(IFilePath fp, CancellationToken cancellationToken = default)
    {
        using (HeavyProfiler.Log("AzureBlobStorage SaveFile"))
        using (new EntityCache(EntityCacheType.ForceNew))
        {
            if (WeakFileReference)
                return Task.CompletedTask;

            BlobContainerClient client = CalculateSuffixWithRenames(fp);

            try
            {
                var headers = GetBlobHttpHeaders(fp, this.GetBlobAction(fp));
                var blobClient = client.GetBlobClient(fp.Suffix);

                var binaryFile = fp.BinaryFile;
                //fp.CleanBinaryFile(); at the end of transaction
                var azureDefenderPollingOptions = this.AzureDefenderPolling?.Invoke(fp);
                return SaveFileInAzureAsync(blobClient, binaryFile, headers, fp.Suffix, azureDefenderPollingOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                ex.Data.Add("Suffix", fp.Suffix);
                ex.Data.Add("AccountName", client.AccountName);
                ex.Data.Add("ContainerName", client.Name);
                throw;
            }
        }
    }

    static async Task SaveFileInAzureAsync(BlobClient blobClient, byte[] binaryFile, BlobHttpHeaders headers, string suffixForException, AzureDefenderPollingOptions? azureDefenderPollingOptions, CancellationToken cancellationToken)
    {
        try
        {
            await blobClient.UploadAsync(new MemoryStream(binaryFile), httpHeaders: headers, cancellationToken: cancellationToken);
            if (azureDefenderPollingOptions != null)
            {
                await CheckBlobStorageFileForWindowsDefenderLogs(blobClient, azureDefenderPollingOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            ex.Data.Add("Suffix", suffixForException);
            ex.Data.Add("AccountName", blobClient.AccountName);
            ex.Data.Add("ContainerName", blobClient.Name);
            throw;
        }
    }

    public async Task<string?> StartUpload(IFilePath fp, CancellationToken token = default)
    {
        using (HeavyProfiler.Log("AzureBlobStorage StartUpload"))
        using (new EntityCache(EntityCacheType.ForceNew))
        {
            BlobContainerClient client = CalculateSuffixWithRenames(fp);
            try
            {
                var headers = GetBlobHttpHeaders(fp, this.GetBlobAction(fp));
                var blobClient = client.GetBlobClient(fp.Suffix);
                if (await blobClient.ExistsAsync(token))
                    throw new InvalidOperationException("File already exists");
                await blobClient.UploadAsync(new MemoryStream(), overwrite: true, cancellationToken: token);
                return null; // Azure does not use uploadId
            }
            catch (Exception ex)
            {
                ex.Data.Add("Suffix", fp.Suffix);
                ex.Data.Add("AccountName", client.AccountName);
                ex.Data.Add("ContainerName", client.Name);
                throw;
            }
        }
    }

    public async Task<ChunkInfo> UploadChunk(IFilePath fp, int chunkIndex, MemoryStream chunk, string? uploadId = null, CancellationToken token = default)
    {
        using (HeavyProfiler.Log("AzureBlobStorage UploadChunk"))
        using (new EntityCache(EntityCacheType.ForceNew))
        {
            BlobContainerClient client = GetClient(fp);
            try
            {
                var blockClient = client.GetBlockBlobClient(fp.Suffix);
                string blockIdb64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(chunkIndex.ToString("D6")));
                chunk.Position = 0;
                await blockClient.StageBlockAsync(blockIdb64, chunk, cancellationToken: token);
                chunk.Position = 0;
                return new ChunkInfo
                {
                    PartialHash = CryptorEngine.CalculateMD5Hash(chunk),
                    BlockId = blockIdb64,
                };
            }
            catch (Exception ex)
            {
                ex.Data.Add("Suffix", fp.Suffix);
                ex.Data.Add("AccountName", client.AccountName);
                ex.Data.Add("ContainerName", client.Name);
                throw;
            }
        }
    }

    public async Task FinishUpload(IFilePath fp, List<ChunkInfo> chunks, string? uploadId = null, CancellationToken token = default)
    {
        using (HeavyProfiler.Log("AzureBlobStorage FinishUpload"))
        {
            BlobContainerClient client = GetClient(fp);
            try
            {
                var blockClient = client.GetBlockBlobClient(fp.Suffix);

                await blockClient.CommitBlockListAsync(chunks.Select(a => a.BlockId).ToList(), cancellationToken: token);

                var properties = await blockClient.GetPropertiesAsync(cancellationToken: token);
                var hashList = chunks.ToString(c => c.PartialHash, "\n");
                fp.FileLength = properties.Value.ContentLength;
                fp.Hash = CryptorEngine.CalculateMD5Hash(Encoding.UTF8.GetBytes(hashList));
            }
            catch (Exception ex)
            {
                ex.Data.Add("Suffix", fp.Suffix);
                ex.Data.Add("AccountName", client.AccountName);
                ex.Data.Add("ContainerName", client.Name);
                throw;
            }
        }
    }

    public Task AbortUpload(IFilePath fp, string? uploadId = null, CancellationToken token = default)
    {
        // Azure does not require explicit abort for block blobs
        return Task.CompletedTask;
    }

    string? blobContainerAlreadyCreated;
    private BlobContainerClient CalculateSuffixWithRenames(IFilePath fp)
    {
        using (HeavyProfiler.LogNoStackTrace("CalculateSuffixWithRenames"))
        {
            string suffix = CalculateSuffix(fp);
            if (!suffix.HasText())
                throw new InvalidOperationException("Suffix not set");

            var client = GetClient(fp);
            if (CreateBlobContainerIfNotExists && blobContainerAlreadyCreated != client.Name)
            {
                using (HeavyProfiler.LogNoStackTrace("AzureBlobStorage CreateIfNotExists"))
                {
                    try
                    {
                        client.CreateIfNotExists();
                        blobContainerAlreadyCreated = client.Name;
                    }
                    catch (Azure.RequestFailedException ex) when (ex.ErrorCode == "ContainerAlreadyExists")
                    {
                        // Ignore concurrency: container exists
                        blobContainerAlreadyCreated = client.Name;
                    }
                }
            }

            int i = 2;
            fp.Suffix = suffix.Replace("\\", "/");

            if (RenameAlgorithm != null)
            {
                while (HeavyProfiler.LogNoStackTrace("ExistBlob").Using(_ => client.ExistsBlob(fp.Suffix)))
                {
                    fp.Suffix = RenameAlgorithm(suffix, i).Replace("\\", "/");
                    i++;
                }
            }

            return client;
        }
    }

    public Func<IFilePath, string?>? GetCacheControl = null;

    private BlobHttpHeaders GetBlobHttpHeaders(IFilePath fp, BlobAction action)
    {
        var contentType = action == BlobAction.Download ? "application/octet-stream" :
                FileTypeContentTypes.ContentTypes.TryGet(Path.GetExtension(fp.FileName).ToLowerInvariant(), "application/octet-stream");

        return new BlobHttpHeaders
        {
            ContentType = contentType,
            ContentDisposition = action == BlobAction.Download ? "attachment" : "inline",
            CacheControl = GetCacheControl?.Invoke(fp) ?? "",
        };
    }

    public void MoveFile(IFilePath ofp, IFilePath nfp, bool createTargetFolder)
    {
        using (HeavyProfiler.Log("AzureBlobStorage MoveFile", () => ofp.FileName))
        {
            if (WeakFileReference)
                return;

            throw new NotImplementedException();
        }
    }

    public void DeleteFiles(IEnumerable<IFilePath> files)
    {
        using (HeavyProfiler.Log("AzureBlobStorage DeleteFiles"))
        {
            if (WeakFileReference)
                return;

            foreach (var f in files)
            {
                GetClient(f).DeleteBlob(f.Suffix);
            }
        }
    }

    public void DeleteFilesIfExist(IEnumerable<IFilePath> files)
    {
        using (HeavyProfiler.Log("AzureBlobStorage DeleteFiles"))
        {
            if (WeakFileReference)
                return;

            foreach (var f in files)
            {
                GetClient(f).DeleteBlobIfExists(f.Suffix);
            }
        }
    }

    public void UpdateHttpHeaders(IFilePath fp)
    {
        BlobContainerClient client = GetClient(fp);

        BlobHttpHeaders headers = GetBlobHttpHeaders(fp, this.GetBlobAction(fp));
        BlobClient blobClient = client.GetBlobClient(fp.Suffix);

        blobClient.SetHttpHeaders(headers);
    }

    static async Task CheckBlobStorageFileForWindowsDefenderLogs(BlobClient blobClient, AzureDefenderPollingOptions options, CancellationToken token)
    {
        try
        {
            var pollTime = options.TotalWaitTime;
            var pollInterval = options.PollInterval;
            var fileName = Path.GetFileName(blobClient.Name);

            while (pollTime > TimeSpan.Zero)
            {
                pollTime -= pollInterval;
                var tagsResponse = await blobClient.GetTagsAsync();
                var tags = tagsResponse.Value.Tags;
                if (tags.TryGetValue("Malware Scanning scan result", out var status))
                {
                    switch (status)
                    {
                        case "No threats found":
                            return;
                        case "Malicious":                            
                            throw new MicrosoftDefenderMaliciousFileFoundException(fileName);
                        default:
                            throw new UnexpectedValueException(status);
                    }
                }
                await Task.Delay(pollInterval, token);
            }
        }
        catch
        {
            // remove potentially malicious file
            await blobClient.DeleteIfExistsAsync();
            throw;
        }
    }
}

public static class BlobExtensions
{
    public static bool ExistsBlob(this BlobContainerClient client, string blobName)
    {
        return client.GetBlobs(prefix: blobName.BeforeLast("/") ?? "").Any(b => b.Name == blobName);
    }
}

public class AzureDefenderPollingOptions
{
    public TimeSpan TotalWaitTime { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(3);
}

public sealed class MicrosoftDefenderMaliciousFileFoundException : Exception
{
    public MicrosoftDefenderMaliciousFileFoundException(string fileName)
        : base(FileMessage.File0ContainsAThreatBy1.NiceToString(fileName, "Microsoft Defender"))
    {
    }
}
