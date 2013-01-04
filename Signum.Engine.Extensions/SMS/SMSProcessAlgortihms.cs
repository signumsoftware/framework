using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Processes;
using Signum.Entities.SMS;
using Signum.Entities;
using Signum.Engine.Operations;
using Signum.Engine;
using Signum.Entities.Processes;

namespace Signum.Engine.SMS
{
    public class SMSMessageSendProcessAlgortihm : IProcessAlgorithm
    {
        public Entities.Processes.IProcessDataDN CreateData(object[] args)
        {
            throw new NotImplementedException();
        }

        public int NotificationSteps = 100;

        public void Execute(IExecutingProcess executingProcess)
        {
            SMSSendPackageDN package = (SMSSendPackageDN)executingProcess.Data;

            List<Lite<SMSMessageDN>> messages = (from message in Database.Query<SMSMessageDN>()
                                                 where message.SendPackage == package.ToLite() &&
                                                 message.State == SMSMessageState.Created
                                                 select message.ToLite()).ToList();

            for (int i = 0; i < messages.Count; i++)
            {
                executingProcess.CancellationToken.ThrowIfCancellationRequested();

                SMSMessageDN ms = messages[i].RetrieveAndForget();

                try
                {
                    ms.Execute(SMSMessageOperation.Send);
                }
                catch (Exception)
                {
                    package.NumErrors++;
                    package.Save();
                }

                executingProcess.ProgressChanged(i, messages.Count);
            }
        }
    }

    public class SMSMessageUpdateStatusProcessAlgorithm : IProcessAlgorithm
    {
        public Entities.Processes.IProcessDataDN CreateData(object[] args)
        {
            SMSUpdatePackageDN package = new SMSUpdatePackageDN().Save();

            package.NumLines = (from m in Database.Query<SMSMessageDN>()
                                where m.State == SMSMessageState.Sent && m.UpdatePackage == null
                                select m).UnsafeUpdate(a => new SMSMessageDN { UpdatePackage = package.ToLite() });

            return package.Save();
        }

        public int NotificationSteps = 100;

        public void Execute(IExecutingProcess executingProcess)
        {
            SMSUpdatePackageDN package = (SMSUpdatePackageDN)executingProcess.Data;

            List<Lite<SMSMessageDN>> messages = (from message in Database.Query<SMSMessageDN>()
                                                 where message.UpdatePackage == package.ToLite() &&
                                                 message.State == SMSMessageState.Sent
                                                 select message.ToLite()).ToList();

            for (int i = 0; i < messages.Count; i++)
            {
                executingProcess.CancellationToken.ThrowIfCancellationRequested();

                SMSMessageDN ms = messages[i].RetrieveAndForget();

                try
                {
                    ms.Execute(SMSMessageOperation.UpdateStatus);
                }
                catch (Exception)
                {
                    package.NumErrors++;
                    package.Save();
                }

                executingProcess.ProgressChanged(i, messages.Count);
            }
        }
    }
}
