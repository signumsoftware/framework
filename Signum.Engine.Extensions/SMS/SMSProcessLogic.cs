using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.Processes;
using Signum.Engine.Scheduler;
using Signum.Entities;
using Signum.Entities.Processes;
using Signum.Entities.SMS;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Signum.Engine.SMS
{
    public static class SMSProcessLogic
    {

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SMSSendPackageEntity>();
                sb.Include<SMSUpdatePackageEntity>();
                SMSLogic.AssertStarted(sb);
                ProcessLogic.AssertStarted(sb);
                ProcessLogic.Register(SMSMessageProcess.Send, new SMSMessageSendProcessAlgortihm());
                ProcessLogic.Register(SMSMessageProcess.UpdateStatus, new SMSMessageUpdateStatusProcessAlgorithm());
                SimpleTaskLogic.Register(SMSMessageTask.UpdateSMSStatus, ctx => UpdateAllSentSMS()?.ToLite());

                new Graph<ProcessEntity>.ConstructFromMany<SMSMessageEntity>(SMSMessageOperation.CreateUpdateStatusPackage)
                {
                    Construct = (messages, _) => UpdateMessages(messages.RetrieveFromListOfLite())
                }.Register();

                QueryLogic.Queries.Register(typeof(SMSSendPackageEntity), () =>
                    from e in Database.Query<SMSSendPackageEntity>()
                    let p = e.LastProcess()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        NumLines = e.SMSMessages().Count(),
                        LastProcess = p,
                        NumErrors = e.SMSMessages().Count(s => s.Exception(p) != null),
                    });

                QueryLogic.Queries.Register(typeof(SMSUpdatePackageEntity), () =>
                    from e in Database.Query<SMSUpdatePackageEntity>()
                    let p = e.LastProcess()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        NumLines = e.SMSMessages().Count(),
                        LastProcess = p,
                        NumErrors = e.SMSMessages().Count(s => s.Exception(p) != null),
                    });
            }
        }

        public static void RegisterSMSOwnerData<T>(Expression<Func<T, SMSOwnerData>> phoneExpression) where T : Entity
        {
            new Graph<ProcessEntity>.ConstructFromMany<T>(SMSMessageOperation.SendMultipleSMSMessages)
            {
                Construct = (providers, args) =>
                {
                    var sMSOwnerDatas = Database.Query<T>().Where(p => providers.Contains(p.ToLite()))
                        .Select(pr => phoneExpression.Evaluate(pr))
                        .AsEnumerable().NotNull().Distinct().ToList();

                    MultipleSMSModel model = args.GetArg<MultipleSMSModel>();

                    IntegrityCheck? ic = model.IntegrityCheck();

                    if (!model.Message.HasText())
                        throw new ApplicationException("The text for the SMS message has not been set");

                    SMSSendPackageEntity package = new SMSSendPackageEntity().Save();

                    var packLite = package.ToLite();

                    using (OperationLogic.AllowSave<SMSMessageEntity>())
                        (from od in sMSOwnerDatas
                         from n in od.TelephoneNumber.SplitNoEmpty(",")
                         select new SMSMessageEntity
                         {
                             DestinationNumber = n,
                             SendPackage = packLite,
                             Referred = od.Owner,

                             Message = model.Message,
                             From = model.From,
                             Certified = model.Certified,
                             State = SMSMessageState.Created,
                         }).SaveList();

                    var process = ProcessLogic.Create(SMSMessageProcess.Send, package);

                    process.Execute(ProcessOperation.Execute);

                    return process;
                }
            }.Register();
        }



        private static ProcessEntity UpdateMessages(List<SMSMessageEntity> messages)
        {
            SMSUpdatePackageEntity package = new SMSUpdatePackageEntity().Save();

            var packLite = package.ToLite();

            if (messages.Any(m => m.State != SMSMessageState.Sent))
                throw new ApplicationException("SMS messages must be sent prior to update the status");

            messages.ForEach(ms => ms.UpdatePackage = packLite);
            messages.SaveList();

            var process = ProcessLogic.Create(SMSMessageProcess.UpdateStatus, package);

            process.Execute(ProcessOperation.Execute);

            return process;
        }

        public static ProcessEntity? UpdateAllSentSMS()
        {
            var messages = Database.Query<SMSMessageEntity>().Where(a => a.State == SMSMessageState.Sent);

            if (!messages.Any())
                return null;

            SMSUpdatePackageEntity package = new SMSUpdatePackageEntity();
            package.Save();
            messages.UnsafeUpdate().Set(a => a.UpdatePackage, a => package.ToLite()).Execute();
            return SMSMessageProcess.UpdateStatus.Create(package).Execute(ProcessOperation.Execute);
        }


    }
}
