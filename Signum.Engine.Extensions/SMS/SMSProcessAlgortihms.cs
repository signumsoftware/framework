using System.Linq;
using Signum.Engine.Processes;
using Signum.Entities.SMS;
using Signum.Engine.Operations;

namespace Signum.Engine.SMS
{
    public class SMSMessageSendProcessAlgortihm : IProcessAlgorithm
    {
        public void Execute(ExecutingProcess executingProcess)
        {
            SMSSendPackageEntity package = (SMSSendPackageEntity)executingProcess.Data!;

            executingProcess.ForEachLine(package.SMSMessages().Where(s => s.State == SMSMessageState.Created),
                sms => sms.Execute(SMSMessageOperation.Send));
        }
    }

    public class SMSMessageUpdateStatusProcessAlgorithm : IProcessAlgorithm
    {
        public void Execute(ExecutingProcess executingProcess)
        {
            SMSUpdatePackageEntity package = (SMSUpdatePackageEntity)executingProcess.Data!;

            executingProcess.ForEachLine(package.SMSMessages().Where(sms => sms.State == SMSMessageState.Sent && !sms.UpdatePackageProcessed), sms =>
            {
                sms.Execute(SMSMessageOperation.UpdateStatus);
            });
        }
    }
}
