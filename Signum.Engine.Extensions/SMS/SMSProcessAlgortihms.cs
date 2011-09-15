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
            SMSPackageDN package = (SMSPackageDN)executingProcess.Data;

            List<Lite<SMSMessageDN>> messages = (from message in Database.Query<SMSMessageDN>()
                                                 where message.SendPackage == package.ToLite() &&
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
                    ms.Execute(SMSMessageOperations.Send);
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

            return FinalState.Finished;
        }
    }

    public class SMSMessageUpdateStatusProcessAlgorithm : IProcessAlgorithm
    {
        public Entities.Processes.IProcessDataDN CreateData(object[] args)
        {
            SMSPackageDN package = new SMSPackageDN().Save();

            package.NumLines = (from m in Database.Query<SMSMessageDN>()
                                where m.State == SMSMessageState.Sent
                                    && m.SendState != SendState.Delivered
                                    && m.SendState != SendState.Failed
                                    && m.UpdatePackage == null
                                select m).UnsafeUpdate(a => new SMSMessageDN { UpdatePackage = package.ToLite() });

            return package.Save();
        }

        public int NotificationSteps = 100;

        public FinalState Execute(IExecutingProcess executingProcess)
        {
            SMSPackageDN package = (SMSPackageDN)executingProcess.Data;

            List<Lite<SMSMessageDN>> messages = (from message in Database.Query<SMSMessageDN>()
                                                 where message.UpdatePackage == package.ToLite() &&
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

            return FinalState.Finished;
        }
    }
}
