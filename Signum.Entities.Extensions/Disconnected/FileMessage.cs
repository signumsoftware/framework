using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.IO;
using Signum.Services;

namespace Signum.Entities.Disconnected
{
    [MessageContract]
    public class FileMessage : IDisposable
    {
        [MessageHeader(MustUnderstand = true)]
        public string FileName;

        [MessageHeader(MustUnderstand = true)]
        public long Length;

        [MessageBodyMember(Order = 1)]
        public Stream Stream;

        public void Dispose()
        {
            if (Stream != null)
            {
                Stream.Close();
                Stream = null;

                if (OnDisposing != null)
                    OnDisposing();
            }
        }

        public Action OnDisposing;

        public FileMessage() { }
        public FileMessage(string file) 
        {
            FileInfo fi = new FileInfo(file);

            FileName = file;
            Length = fi.Length;
            Stream = fi.OpenRead();
        }
    }

    [MessageContract]
    public class UploadDatabaseResult
    {
        [MessageHeader(MustUnderstand = true)]
        public Lite<UploadStatisticsDN> UploadStatistics;
    }

    [MessageContract]
    public class DownloadDatabaseRequests
    {
        [MessageHeader(MustUnderstand = true)]
        public Lite<DownloadStatisticsDN> DownloadStatistics;
    }

    [ServiceContract]
    public interface IDisconnectedTransferServer
    {
        [OperationContract, NetDataContractAttribute]
        UploadDatabaseResult UploadDatabase(FileMessage request);

        [OperationContract, NetDataContractAttribute]
        Lite<DownloadStatisticsDN> BeginExportDatabase(Lite<DisconnectedMachineDN> machine);

        [OperationContract, NetDataContractAttribute]
        FileMessage EndExportDatabase(DownloadDatabaseRequests statistics);
    }

    [ServiceContract]
    public interface IDisconnectedServer
    {
        [OperationContract, NetDataContractAttribute]
        DownloadStatisticsDN GetDownloadEstimation(Lite<DisconnectedMachineDN> machine);

        [OperationContract, NetDataContractAttribute]
        List<Lite<DisconnectedMachineDN>> CurrentMachines();

        [OperationContract, NetDataContractAttribute]
        UploadStatisticsDN GetUploadEstimation(Lite<DisconnectedMachineDN> machine);
    }
}
