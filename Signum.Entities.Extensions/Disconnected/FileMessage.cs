using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.IO;
using Signum.Services;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;

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

                OnDisposing?.Invoke();
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
        public Lite<DisconnectedImportEntity> UploadStatistics;
    }

    [MessageContract]
    public class DownloadDatabaseRequests
    {
        [MessageHeader(MustUnderstand = true)]
        public Lite<DisconnectedExportEntity> DownloadStatistics;

        [MessageHeader(MustUnderstand = true)]
        public Lite<IUserEntity> User;
    }

    [MessageContract]
    public class UploadDatabaseRequest: FileMessage
    {
        [MessageHeader(MustUnderstand = true)]
        public Lite<IUserEntity> User;

        [MessageHeader(MustUnderstand = true)]
        public Lite<DisconnectedMachineEntity> Machine; 
    }

    [ServiceContract]
    public interface IDisconnectedTransferServer
    {
        [OperationContract, NetDataContractAttribute]
        UploadDatabaseResult UploadDatabase(UploadDatabaseRequest request);

        [OperationContract, NetDataContractAttribute]
        Lite<DisconnectedExportEntity> BeginExportDatabase(Lite<IUserEntity> user, Lite<DisconnectedMachineEntity> machine);

        [OperationContract, NetDataContractAttribute]
        FileMessage EndExportDatabase(DownloadDatabaseRequests request);
    }

    [ServiceContract]
    public interface IDisconnectedServer
    {
        [OperationContract, NetDataContractAttribute]
        DisconnectedExportEntity GetDownloadEstimation(Lite<DisconnectedMachineEntity> machine);

        [OperationContract, NetDataContractAttribute]
        Lite<DisconnectedMachineEntity> GetDisconnectedMachine(string machineName);

        [OperationContract, NetDataContractAttribute]
        DisconnectedImportEntity GetUploadEstimation(Lite<DisconnectedMachineEntity> machine);

        [OperationContract, NetDataContractAttribute]
        Dictionary<Type, StrategyPair> GetStrategyPairs();

        [OperationContract, NetDataContractAttribute]
        void SkipExport(Lite<DisconnectedMachineEntity> machine);

        [OperationContract, NetDataContractAttribute]
        void ConnectAfterFix(Lite<DisconnectedMachineEntity> machine);
    }
}
