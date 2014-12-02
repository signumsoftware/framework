using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Services;
using System.ServiceModel;
using Signum.Entities.Files;
using System.ComponentModel;

namespace Signum.Services
{
    [ServiceContract]
    public interface IFileServer
    {
        [OperationContract, NetDataContract]
        byte[] GetBinaryFile(IFile file);

        /*
        byte[] GetBinaryFile(IFile file)
        {
            return Return(MethodInfo.GetCurrentMethod(), () =>
                {
                    if (file is FilePathEntity)
                        return Signum.Engine.Files.FilePathLogic.GetByteArray((FilePathEntity)file);
                    throw new InvalidOperationException("The file type is not known or this method must not be invoked for it");
                });
        }       
        */
    }

}
