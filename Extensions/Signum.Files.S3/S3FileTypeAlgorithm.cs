using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using System.Threading;
using Signum.Files;
using Amazon.S3.Util;
using Signum.Utilities.Synchronization;
using System.Net;

namespace Signum.Files.S3;

public enum S3WebDownload
{
    PreSignedUrl,
    DirectUrl,
    None
}

public class S3FileTypeAlgorithm : FileTypeAlgorithmBase, IFileTypeAlgorithm
{
    public IAmazonS3 Client { get; private set; }
    
    //Bucket mode
    //SubDirectory mode
    public string? SharedBucketName { get; set; }
    public Func<IFilePath, string>? GetBucketNameOrSubDirectory { get; private set; }
    
    public Func<S3WebDownload> WebDownload { get; set; } = () => S3WebDownload.None;
    public Func<IFilePath, string> CalculateKey { get; set; } = SuffixGenerators.Safe.YearMonth_Guid_Filename;
    public bool WeakFileReference { get; set; }

    // New: automatically create bucket on first use
    public bool CreateBucketIfNotExists { get; set; }

    // Simplified: remember only the last bucket we created
    private string? lastCreatedBucket;

    // Optional: rename algorithm to avoid collisions
    public Func<string, int, string>? RenameAlgorithm { get; set; } = null; // FileTypeAlgorithm.DefaultRenameAlgorithm;

    public S3FileTypeAlgorithm(IAmazonS3 client, string? sharedBucketName, Func<IFilePath, string> getBucketNameOrSubDirectory)
    {
        this.Client = client;
        this.SharedBucketName = sharedBucketName;
        this.GetBucketNameOrSubDirectory = getBucketNameOrSubDirectory;
    }


    public string? GetFullPhysicalPath(IFilePath efp) => null;


    public (string bucket, string key) GetBucketAndKey(IFilePath fp)
    {
        if (SharedBucketName.HasText())
        {
            var bucket = SharedBucketName;
            var key = this.GetBucketNameOrSubDirectory!(fp) + "/" + fp.Suffix;
            return (bucket, key);
        }
        else
        {
            var bucket = this.GetBucketNameOrSubDirectory!(fp);
            var key = fp.Suffix;
            return (bucket, key);

        }
    }

    public string? GetFullWebPath(IFilePath fp)
    {
        var s3download = WebDownload();
        if (s3download == S3WebDownload.None)
            return null;

        var (bucket, key) = this.GetBucketAndKey(fp);
        if (s3download == S3WebDownload.PreSignedUrl)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(15),
                Verb = HttpVerb.GET,
            };
            var result = Client.GetPreSignedURL(request);
            if (Client.Config.UseHttp && result.StartsWith("https:"))
                return "http:" + result.After("https:");
            return result;

        }
        else if (s3download == S3WebDownload.DirectUrl)
        {
            // Public URL logic
            var config = Client.Config as AmazonS3Config;
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
        else
            throw new UnexpectedValueException(s3download);
    }

    void EnsureBucketExists(string bucket)
    {
        if (!CreateBucketIfNotExists)
            return;

        if (lastCreatedBucket == bucket)
            return;

        using (HeavyProfiler.LogNoStackTrace("S3 CreateBucketIfNotExists"))
        {
            try
            {
                var exists = AmazonS3Util.DoesS3BucketExistV2Async(Client, bucket).ResultSafe();
                if (!exists)
                {
                    Client.PutBucketAsync(new PutBucketRequest { BucketName = bucket }).WaitSafe();
                    lastCreatedBucket = bucket;
                }
            }
            catch (AmazonS3Exception as3) when (as3.ErrorCode == "BucketAlreadyOwnedByYou" || as3.ErrorCode == "BucketAlreadyExists")
            {
                // ignore
                lastCreatedBucket = bucket;
            }
        }
    }



    public Stream OpenRead(IFilePath fp)
    {
        using (HeavyProfiler.Log("S3 OpenRead", () => fp.Suffix))
        {
            try
            {

                var (bucket, key) = this.GetBucketAndKey(fp);
                var response = Client.GetObjectAsync(bucket, key).Result;
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

            var (bucket, key) = CalculateKeyWithRenames(fp);
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
                Client.PutObjectAsync(putRequest).Wait();
                fp.CleanBinaryFile();
            }
            catch (Exception ex)
            {
                ex.Data.Add("Suffix", fp.Suffix);
                ex.Data.Add("Key", key);
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

            var (bucket, key) = CalculateKeyWithRenames(fp);
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
                return  Client.PutObjectAsync(putRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                ex.Data.Add("Suffix", fp.Suffix);
                ex.Data.Add("Key", key);
                ex.Data.Add("BucketName", bucket);
                throw;
            }
        }
    }

    public void MoveFile(IFilePath ofp, IFilePath nfp, bool createTargetFolder)
    {
        using (HeavyProfiler.Log("S3 MoveFile", () => ofp.FileName))
        {
            if (WeakFileReference)
                return;

            throw new NotImplementedException();
        }
    }

    public void DeleteFiles(IEnumerable<IFilePath> files)
    {
        foreach (var f in files)
        {
            var (bucket, key) = GetBucketAndKey(f);
            Client.DeleteObjectAsync(bucket, key).Wait();
        }
    }

    public void DeleteFilesIfExist(IEnumerable<IFilePath> files)
    {
        if (WeakFileReference)
            return;

        foreach (var f in files)
        {
            var (bucket, key) = GetBucketAndKey(f);
            try { Client.DeleteObjectAsync(bucket, key).Wait(); } catch { }
        }
    }

    public async Task<string?> StartUpload(IFilePath fp, CancellationToken token = default)
    {
        var (bucket, key) = CalculateKeyWithRenames(fp);
        var request = new InitiateMultipartUploadRequest
        {
            BucketName = bucket,
            Key = key,
            ContentType = FileTypeContentTypes.ContentTypes.TryGet(Path.GetExtension(fp.FileName).ToLowerInvariant(), "application/octet-stream")
        };
        var response = await Client.InitiateMultipartUploadAsync(request, token);
        return response.UploadId;
    }

    private (string bucket, string key) CalculateKeyWithRenames(IFilePath fp)
    {
        using (HeavyProfiler.LogNoStackTrace("CalculateKeyWithRenames"))
        {
            string bucket = SharedBucketName ?? GetBucketNameOrSubDirectory!(fp);
            var directory = SharedBucketName != null ? this.GetBucketNameOrSubDirectory!(fp) + "/" : "";
            EnsureBucketExists(bucket);

            string key = CalculateKey(fp).Replace("\\", "/");
            fp.Suffix = key;

            if (RenameAlgorithm != null)
            {
                int i = 2;
                while (ExistsObject(bucket, directory + fp.Suffix))
                {
                    fp.Suffix = RenameAlgorithm(key, i).Replace("\\", "/");
                    i++;
                }
            }

            return (bucket, directory + fp.Suffix);
        }
    }

    bool ExistsObject(string bucket, string key)
    {
        using (HeavyProfiler.Log("S3 ExistsObject", () => key))
        {
            try
            {
                Client.GetObjectMetadataAsync(bucket, key).WaitSafe();
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }

    public async Task<ChunkInfo> UploadChunk(IFilePath fp, int chunkIndex, MemoryStream chunk, string? uploadId = null, CancellationToken token = default)
    {
        var (bucket, key) = GetBucketAndKey(fp);
        if (uploadId == null)
            throw new ArgumentNullException("uploadId");
        
        chunk.Position = 0;
        var uploadPartRequest = new UploadPartRequest
        {
            BucketName = bucket,
            Key = key,
            UploadId = uploadId!,
            PartNumber = chunkIndex + 1,
            InputStream = chunk,
            IsLastPart = false
        };
        var response = await Client.UploadPartAsync(uploadPartRequest, token);
        chunk.Position = 0;
        return new ChunkInfo
        {
            PartialHash = response.ETag, // S3 ETag for the part
            BlockId = chunkIndex.ToString()
        };
    }

    public async Task FinishUpload(IFilePath fp, List<ChunkInfo> chunks, string? uploadId = null, CancellationToken token = default)
    {
        var (bucket, key) = GetBucketAndKey(fp);

        if (uploadId == null)
            throw new ArgumentNullException("uploadId");

        var partETags = chunks.Select(c => new PartETag(int.Parse(c.BlockId) + 1, c.PartialHash)).ToList();
        var completeRequest = new CompleteMultipartUploadRequest
        {
            BucketName = bucket,
            Key = key,
            UploadId = uploadId!,
            PartETags = partETags
        };
        await Client.CompleteMultipartUploadAsync(completeRequest, token);
    }

    public async Task AbortUpload(IFilePath fp, string? uploadId = null, CancellationToken token = default)
    {
        var (bucket, key) = GetBucketAndKey(fp);

        if (uploadId == null)
            throw new ArgumentNullException("uploadId");

        var abortRequest = new AbortMultipartUploadRequest
        {
            BucketName = bucket,
            Key = fp.Suffix,
            UploadId = uploadId!
        };
        await Client.AbortMultipartUploadAsync(abortRequest, token);
    }
}
