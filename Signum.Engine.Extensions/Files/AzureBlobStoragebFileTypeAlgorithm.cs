
using Azure.Storage.Blobs;
using Signum.Entities.Files;
using Signum.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

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
        Func<BlobContainerClient> getClient;
        Func<bool> webDownload;
        public AzureBlobStoragebFileTypeAlgorithm(Func<BlobContainerClient> getClient, Func<bool> webDownload)
        {
            this.getClient = getClient;
            this.webDownload = webDownload;
        }

        public string? ConfigErrors()
        {
            return null;
        }

        public void DeleteFiles(IEnumerable<IFilePath> files)
        {
            var client = getClient();
            foreach (var f in files)
            {
                client.DeleteBlob(f.Suffix);
            }
        }

        public PrefixPair GetPrefixPair(IFilePath efp)
        {
            var client = getClient();

            if (!this.webDownload())
                return PrefixPair.None();

            return PrefixPair.WebOnly($"https://{client.Uri}/{efp.Suffix}");
        }

        public void MoveFile(IFilePath ofp, IFilePath nfp)
        {
            throw new NotImplementedException("");
        }

        public Stream OpenRead(IFilePath fp)
        {
            var client = getClient();
            return client.GetBlobClient(fp.Suffix).Download().Value.Content;
        }

        public byte[] ReadAllBytes(IFilePath fp)
        {
            var client = getClient();
            return client.GetBlobClient(fp.Suffix).Download().Value.Content.ReadAllBytes();
        }

        public void SaveFile(IFilePath fp)
        {
            var client = getClient();
            client.GetBlobClient(fp.Suffix).Upload(new MemoryStream(fp.BinaryFile));

        }
    }

}
