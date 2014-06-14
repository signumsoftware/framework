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
        public void Execute(ExecutingProcess executingProcess)
        {
            SMSSendPackageDN package = (SMSSendPackageDN)executingProcess.Data;

            executingProcess.ForEachLine(package.SMSMessages().Where(s => s.State == SMSMessageState.Created),
                sms => sms.Execute(SMSMessageOperation.Send));
        }
    }

    public class SMSMessageUpdateStatusProcessAlgorithm : IProcessAlgorithm
    {
        public void Execute(ExecutingProcess executingProcess)
        {
            SMSUpdatePackageDN package = (SMSUpdatePackageDN)executingProcess.Data;

            executingProcess.ForEachLine(package.SMSMessages().Where(sms => sms.State == SMSMessageState.Sent && !sms.UpdatePackageProcessed), sms =>
            {
                sms.Execute(SMSMessageOperation.UpdateStatus);
            });
        }
    }
}
