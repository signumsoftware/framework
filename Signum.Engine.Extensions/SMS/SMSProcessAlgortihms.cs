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

namespace Signum.Engine.Extensions.SMS
{
    public class SMSMessageSendProcessAlgortihm : IProcessAlgorithm
    {
        public Entities.Processes.IProcessDataDN CreateData(object[] args)
        {
            throw new NotImplementedException();
        }

        public int NotificationSteps = 100;

        public FinalState Execute(IExecutingProcess executingProcess)
        {
            SMSSendPackageDN package = (SMSSendPackageDN)executingProcess.Data;

            List<Lite<SMSMessageDN>> messages = (from message in Database.Query<SMSMessageDN>()
                                                 where message.Package == package.ToLite() &&
                                                 message.State == SMSMessageState.Created
                                                 select message.ToLite()).ToList();

            int lastPercentage = 0;
            for (int i = 0; i < messages.Count; i++)
            {
                if (executingProcess.Suspended)
                    return FinalState.Suspended;

                SMSMessageDN ms = messages[i].RetrieveAndForget();

                try
                {
                    ms.ToLite().ExecuteLite(SMSMessageOperations.Send);
                }
                catch (Exception)
                {
                    package.NumErrors++;
                    package.Save();
                }

                int percentage = (NotificationSteps * i) / messages.Count;
                if (percentage != lastPercentage)
                {
                    executingProcess.ProgressChanged(percentage * 100 / NotificationSteps);
                    lastPercentage = percentage;
                }
            }

            throw new NotImplementedException();
        }
    }

    public class SMSMessageUpdateStatusProcessAlgortihm : IProcessAlgorithm
    {

        public Entities.Processes.IProcessDataDN CreateData(object[] args)
        {
            SMSUpdatePackageDN package = new SMSUpdatePackageDN().Save();

            //TODO: 777 luis - comprobar esta query y modificar para deshacer el doble from
            package.NumLines = (from m in Database.Query<SMSMessageDN>()
                                where m.State == SMSMessageState.Sent
                                    && m.SendState != SendState.Delivered
                                    && m.SendState != SendState.Failed
                                    && (m.UpdatePackage == null
                                    || !Database.Query<ProcessExecutionDN>().Any(pe => 
                                        pe.ProcessData.ToLite().Is(m.UpdatePackage.ToLite<IProcessDataDN>())
                                        && (pe.State == ProcessState.Canceled
                                        || pe.State == ProcessState.Error
                                        || pe.State == ProcessState.Finished)))
                                select m).UnsafeUpdate(a => new SMSMessageDN { UpdatePackage = package.ToLite() });

            return package.Save();
        }

        public int NotificationSteps = 100;

        public FinalState Execute(IExecutingProcess executingProcess)
        {
            SMSSendPackageDN package = (SMSSendPackageDN)executingProcess.Data;

            List<Lite<SMSMessageDN>> messages = (from message in Database.Query<SMSMessageDN>()
                                                 where message.Package == package.ToLite() &&
                                                 message.SendState == SendState.None
                                                 select message.ToLite()).ToList();

            int lastPercentage = 0;
            for (int i = 0; i < messages.Count; i++)
            {
                if (executingProcess.Suspended)
                    return FinalState.Suspended;

                SMSMessageDN ms = messages[i].RetrieveAndForget();

                try
                {
                    ms.ToLite().ExecuteLite(SMSMessageOperations.UpdateStatus);
                }
                catch (Exception)
                {
                    package.NumErrors++;
                    package.Save();
                }

                int percentage = (NotificationSteps * i) / messages.Count;
                if (percentage != lastPercentage)
                {
                    executingProcess.ProgressChanged(percentage * 100 / NotificationSteps);
                    lastPercentage = percentage;
                }
            }

            throw new NotImplementedException();
        }
    }
}
