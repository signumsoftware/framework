
using Azure;
using Azure.Storage.Blobs;
using Signum.Entities.Files;
using Signum.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Signum.Engine.Files
{
    public static class BlobContainerClientPool
    {
        static ConcurrentDictionary<(string connectionString, string blobContainerName), BlobContainerClient> Pool = 
            new ConcurrentDictionary<(string connectionString, string blobContainerName), BlobContainerClient>();

        public static BlobContainerClient Get(string connectionString, string blobContainerName)
        {
            return Pool.GetOrAdd((connectionString, blobContainerName), t => new BlobContainerClient(t.connectionString, t.blobContainerName));
        }
    }

    public class AzureBlobStoragebFileTypeAlgorithm : FileTypeAlgorithmBase, IFileTypeAlgorithm
    {
        public Func<BlobContainerClient> GetClient { get; private set; }
        public Func<bool> WebDownload { get; private set; }

        public Func<IFilePath, string> CalculateSuffix { get; set; }
        public bool RenameOnCollision { get; set; }
        public bool WeakFileReference { get; set; }

        public Func<string, int, string> RenameAlgorithm { get; set; }


        public AzureBlobStoragebFileTypeAlgorithm(Func<BlobContainerClient> getClient, Func<bool> webDownload)
        {
            this.GetClient = getClient;
            this.WebDownload = webDownload;

            CalculateSuffix = SuffixGenerators.Safe.YearMonth_Guid_Filename;
            RenameOnCollision = true;
            RenameAlgorithm = FileTypeAlgorithm.DefaultRenameAlgorithm;
        }

        public PrefixPair GetPrefixPair(IFilePath efp)
        {
            var client = GetClient();

            if (!this.WebDownload())
                return PrefixPair.None();

            return PrefixPair.WebOnly($"https://{client.Uri}/{efp.Suffix}");
        }

        public Stream OpenRead(IFilePath fp)
        {
            var client = GetClient();
            return client.GetBlobClient(fp.Suffix).Download().Value.Content;
        }

        public byte[] ReadAllBytes(IFilePath fp)
        {
            var client = GetClient();
            return client.GetBlobClient(fp.Suffix).Download().Value.Content.ReadAllBytes();
        }

        public virtual void SaveFile(IFilePath fp)
        {
            using (new EntityCache(EntityCacheType.ForceNew))
            {
                if (WeakFileReference)
                    return;

                string suffix = CalculateSuffix(fp);
                if (!suffix.HasText())
                    throw new InvalidOperationException("Suffix not set");

                
                fp.SetPrefixPair(GetPrefixPair(fp));

                var client = GetClient();

                int i = 2;
                fp.Suffix = suffix.Replace("\\", "/");

                while (RenameOnCollision && client.ExistsBlob(fp.Suffix))
                {
                    fp.Suffix = RenameAlgorithm(suffix, i).Replace("\\", "/");
                    i++;
                }

                try
                {
                    client.GetBlobClient(fp.Suffix).Upload(new MemoryStream(fp.BinaryFile));
                }
                catch (Exception ex)
                {
                    ex.Data.Add("Suffix", fp.Suffix);
                    ex.Data.Add("AccountName", client.AccountName);
                    ex.Data.Add("ContainerName", client.Name);
                }
            }
        }

        public void MoveFile(IFilePath ofp, IFilePath nfp)
        {
            if (WeakFileReference)
                return;

            throw new NotImplementedException();
        }

        public void DeleteFiles(IEnumerable<IFilePath> files)
        {
            if (WeakFileReference)
                return;

            var client = GetClient();
            foreach (var f in files)
            {
                client.DeleteBlob(f.Suffix);
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

}
