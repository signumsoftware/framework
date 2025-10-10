using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using System.Threading;
using Signum.Files;

namespace Signum.Files.S3;

public enum S3WebDownload
{
    PreSignedUrl,
    None
}

public class S3FileTypeAlgorithm : FileTypeAlgorithmBase, IFileTypeAlgorithm
{
    public Func<IFilePath, IAmazonS3> GetClient { get; private set; }
    public Func<IFilePath, string> GetBucketName { get; private set; }
    public Func<S3WebDownload> WebDownload { get; set; } = () => S3WebDownload.None;
    public Func<IFilePath, string> CalculateKey { get; set; } = SuffixGenerators.Safe.YearMonth_Guid_Filename;
    public bool WeakFileReference { get; set; }

    public S3FileTypeAlgorithm(Func<IFilePath, IAmazonS3> getClient, Func<IFilePath, string> getBucketName)
    {
        this.GetClient = getClient;
        this.GetBucketName = getBucketName;
    }

    public override bool OnlyImages
    {
        get { return base.OnlyImages; }
        set { base.OnlyImages = value; }
    }

    public string? GetFullPhysicalPath(IFilePath efp) => null;

    public string? GetFullWebPath(IFilePath efp)
    {
        if (WebDownload() == S3WebDownload.None)
            return null;

        var client = GetClient(efp);
        var bucket = GetBucketName(efp);
        var key = efp.Suffix;

        if (WebDownload() == S3WebDownload.PreSignedUrl)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(15),
                Verb = HttpVerb.GET
            };
            return client.GetPreSignedURL(request);
        }

        // Public URL logic
        var config = (client as AmazonS3Client)?.Config as AmazonS3Config;
        var endpoint = config?.ServiceURL?.TrimEnd('/');

        if (!string.IsNullOrEmpty(endpoint))
        {
            // Path-style: http(s)://endpoint/bucket/key
            return $"{endpoint}/{bucket}/{key}";
        }
        else
        {
            // Default AWS virtual-hosted–style: https://bucket.s3.amazonaws.com/key
            return $"https://{bucket}.s3.amazonaws.com/{key}";
        }
    }

    public Stream OpenRead(IFilePath fp)
    {
        using (HeavyProfiler.Log("S3 OpenRead", () => fp.Suffix))
        {
            try
            {
                var client = GetClient(fp);
                var bucket = GetBucketName(fp);
                var key = fp.Suffix;
                var response = client.GetObjectAsync(bucket, key).Result;
                return response.ResponseStream;
            }
            catch (Exception ex)
            {
                ex.Data["suffix"] = fp.Suffix;
                throw;
            }
        }
    }

    public byte[] ReadAllBytes(IFilePath fp)
    {
        using (HeavyProfiler.Log("S3 ReadAllBytes", () => fp.Suffix))
        {
            try
            {
                using var stream = OpenRead(fp);
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                ex.Data["suffix"] = fp.Suffix;
                throw;
            }
        }
    }

    public virtual void SaveFile(IFilePath fp)
    {
        using (HeavyProfiler.Log("S3 SaveFile", () => fp.Suffix))
        {
            if (WeakFileReference)
                return;
            var client = GetClient(fp);
            var bucket = GetBucketName(fp);
            var key = CalculateKey(fp);
            fp.Suffix = key;
            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = key,
                    InputStream = new MemoryStream(fp.BinaryFile),
                    ContentType = FileTypeContentTypes.ContentTypes.TryGet(Path.GetExtension(fp.FileName).ToLowerInvariant(), "application/octet-stream"),
                    AutoCloseStream = true
                };
                client.PutObjectAsync(putRequest).Wait();
                fp.CleanBinaryFile();
            }
            catch (Exception ex)
            {
                ex.Data.Add("Suffix", fp.Suffix);
                ex.Data.Add("BucketName", bucket);
                throw;
            }
        }
    }

    public virtual Task SaveFileAsync(IFilePath fp, CancellationToken cancellationToken = default)
    {
        using (HeavyProfiler.Log("S3 SaveFileAsync", () => fp.Suffix))
        {
            if (WeakFileReference)
                return Task.CompletedTask;
            var client = GetClient(fp);
            var bucket = GetBucketName(fp);
            var key = CalculateKey(fp);
            fp.Suffix = key;
            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = key,
                    InputStream = new MemoryStream(fp.BinaryFile),
                    ContentType = FileTypeContentTypes.ContentTypes.TryGet(Path.GetExtension(fp.FileName).ToLowerInvariant(), "application/octet-stream"),
                    AutoCloseStream = true
                };
                return client.PutObjectAsync(putRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                ex.Data.Add("Suffix", fp.Suffix);
                ex.Data.Add("BucketName", bucket);
                throw;
            }
        }
    }

    public void DeleteFiles(IEnumerable<IFilePath> files)
    {
        foreach (var f in files)
        {
            var client = GetClient(f);
            var bucket = GetBucketName(f);
            client.DeleteObjectAsync(bucket, f.Suffix).Wait();
        }
    }

    public void DeleteFilesIfExist(IEnumerable<IFilePath> files)
    {
        foreach (var f in files)
        {
            var client = GetClient(f);
            var bucket = GetBucketName(f);
            try { client.DeleteObjectAsync(bucket, f.Suffix).Wait(); } catch { }
        }
    }

    // Store UploadId for each file (in-memory, for demo; use persistent storage in production)
    private readonly Dictionary<string, string> multipartUploadIds = new();

    public async Task<string?> StartUpload(IFilePath fp, CancellationToken token = default)
    {
        var client = GetClient(fp);
        var bucket = GetBucketName(fp);
        var key = CalculateKey(fp);
        fp.Suffix = key;
        var request = new InitiateMultipartUploadRequest
        {
            BucketName = bucket,
            Key = key,
            ContentType = FileTypeContentTypes.ContentTypes.TryGet(Path.GetExtension(fp.FileName).ToLowerInvariant(), "application/octet-stream")
        };
        var response = await client.InitiateMultipartUploadAsync(request, token);
        lock (multipartUploadIds)
            multipartUploadIds[key] = response.UploadId;
        return response.UploadId;
    }

    public async Task<ChunkInfo> UploadChunk(IFilePath fp, int chunkIndex, MemoryStream chunk, string? uploadId = null, CancellationToken token = default)
    {
        var client = GetClient(fp);
        var bucket = GetBucketName(fp);
        var key = fp.Suffix;
        string? actualUploadId = uploadId;
        if (actualUploadId == null)
        {
            lock (multipartUploadIds)
                actualUploadId = multipartUploadIds[key];
        }
        chunk.Position = 0;
        var uploadPartRequest = new UploadPartRequest
        {
            BucketName = bucket,
            Key = key,
            UploadId = actualUploadId!,
            PartNumber = chunkIndex,
            InputStream = chunk,
            IsLastPart = false
        };
        var response = await client.UploadPartAsync(uploadPartRequest, token);
        chunk.Position = 0;
        return new ChunkInfo
        {
            PartialHash = response.ETag, // S3 ETag for the part
            BlockId = chunkIndex.ToString()
        };
    }

    public async Task FinishUpload(IFilePath fp, List<ChunkInfo> chunks, string? uploadId = null, CancellationToken token = default)
    {
        var client = GetClient(fp);
        var bucket = GetBucketName(fp);
        var key = fp.Suffix;
        string? actualUploadId = uploadId;
        if (actualUploadId == null)
        {
            lock (multipartUploadIds)
                actualUploadId = multipartUploadIds[key];
        }
        var partETags = chunks.Select(c => new PartETag(int.Parse(c.BlockId), c.PartialHash)).ToList();
        var completeRequest = new CompleteMultipartUploadRequest
        {
            BucketName = bucket,
            Key = key,
            UploadId = actualUploadId!,
            PartETags = partETags
        };
        await client.CompleteMultipartUploadAsync(completeRequest, token);
        lock (multipartUploadIds)
            multipartUploadIds.Remove(key);
    }

    public async Task AbortUpload(IFilePath fp, string? uploadId = null, CancellationToken token = default)
    {
        var client = GetClient(fp);
        var bucket = GetBucketName(fp);
        var key = fp.Suffix;
        string? actualUploadId = uploadId;
        if (actualUploadId == null)
        {
            lock (multipartUploadIds)
                actualUploadId = multipartUploadIds[key];
        }
        var abortRequest = new AbortMultipartUploadRequest
        {
            BucketName = bucket,
            Key = key,
            UploadId = actualUploadId!
        };
        await client.AbortMultipartUploadAsync(abortRequest, token);
        lock (multipartUploadIds)
            multipartUploadIds.Remove(key);
    }

    public void MoveFile(IFilePath ofp, IFilePath nfp, bool createTargetFolder)
    {
        // Not implemented for S3
        throw new NotImplementedException();
    }

    // Add more methods as needed (e.g., multipart upload, update metadata, etc.)
}
