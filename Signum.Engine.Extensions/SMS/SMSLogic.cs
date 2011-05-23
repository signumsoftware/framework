using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities.SMS;
using Signum.Engine.Extensions.Properties;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Engine.Operations;
using Signum.Engine.Processes;

namespace Signum.Engine.SMS
{
    public static class SMSLogic
    {
        static Func<SMSMessageDN, string> SMSSendAction;
        static Func<SMSTemplateDN, List<string>, List<string>> SMSMultipleSendAction;
        static Func<SMSMessageDN, SendState> SMSUpdateStatusAction;

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null, null, false, false)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool registerGraph, bool registerProcess)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SMSMessageDN>();

                dqm[typeof(SMSMessageDN)] = (from m in Database.Query<SMSMessageDN>()
                                             select new
                                             {
                                                 Entity = m.ToLite(),
                                                 m.Id,
                                                 Source = m.From,
                                                 m.DestinationNumber,
                                                 m.SourceNumber,
                                                 m.State,
                                                 m.SendDate,
                                                 m.Template
                                             }).ToDynamic();

                dqm[typeof(SMSTemplateDN)] = (from t in Database.Query<SMSTemplateDN>()
                                              select new
                                              {
                                                  Entity = t.ToLite(),
                                                  t.Id,
                                                  t.Name,
                                                  IsActive = t.IsActiveNow(),
                                                  Message = t.Message.Etc(20),
                                                  Source = t.From,
                                                  t.State,
                                                  t.StartDate,
                                                  t.EndDate,
                                              }).ToDynamic();

                if (registerGraph)
                {
                    SMSMessageGraph.Register();
                    SMSTemplateGraph.Register();
                }

                if (registerProcess)
                {
                    if (!registerGraph)
                        throw new ArgumentException("registerGraph must be true in order to enable operations for the process");

                    ProcessLogic.Register(
                        SMSMessageProcess.Send,
                        new PackageConstructFromAlgorithm<SMSMessageDN, SMSMessageDN>(
                            SMSMessageOperations.Send,
                            () =>
                            {
                                using (new CommandTimeoutScope(300))
                                {
                                    return Database.Query<SMSMessageDN>().Where(m =>
                                        m.State == SMSMessageState.Created).Select(m => m.ToLite()).Take(100).ToList();
                                }
                            }));
                }
            }
        }

        public static void RegisterSMSSendAction(Func<SMSMessageDN, string> action)
        {
            SMSSendAction = action;
        }

        public static void RegisterSMSMultipleSendAction(Func<SMSTemplateDN, List<string>, List<string>> action)
        {
            SMSMultipleSendAction = action;
        }

        public static void RegisterSMSUpdateStatusAction(Func<SMSMessageDN, SendState> action)
        {
            SMSUpdateStatusAction = action;
        }

        public static void SendSMS(SMSMessageDN message)
        {
            if (SMSSendAction == null)
                throw new InvalidOperationException("SMSSendAction was not established");
            SendSMS(message, SMSSendAction);
        }

        //Allows concurrent custom sendProviders for one application
        public static void SendSMS(SMSMessageDN message, Func<SMSMessageDN, string> send)
        {
            message.MessageID = send(message);
            message.SendDate = DateTime.Now.TrimToSeconds();
            message.State = SMSMessageState.Sent;
            message.Save();
        }

        public static List<SMSMessageDN> CreateAndSendMultipleSMSMessages(SMSTemplateDN template, List<string> phones)
        {
            return CreateAndSendMultipleSMSMessages(template, phones, SMSMultipleSendAction);
        }

        //Allows concurrent custom sendProviders for one application
        public static List<SMSMessageDN> CreateAndSendMultipleSMSMessages(SMSTemplateDN template,
            List<string> phones, Func<SMSTemplateDN, List<string>, List<string>> send)
        {
            var messages = new List<SMSMessageDN>();
            var IDs = send(template, phones);
            var sendDate = DateTime.Now.TrimToSeconds();
            for (int i = 0; i < phones.Count; i++)
            {
                var message = template.CreateSMSMessage();
                message.SendDate = sendDate;
                message.SendState = SendState.Sent;
                message.DestinationNumber = phones[i];
                message.MessageID = IDs[i];
                message.Save();
                messages.Add(message);
            }

            return messages;
        }

        public static void UpdateMessageStatus(SMSMessageDN message)
        {
            if (SMSUpdateStatusAction == null)
                throw new InvalidOperationException("SMSUpdateStatusAction was not established");
            UpdateMessageStatus(message, SMSUpdateStatusAction);
        }

        //Allows concurrent custom updateStatusProviders for one application
        public static void UpdateMessageStatus(SMSMessageDN message, Func<SMSMessageDN, SendState> updateAction)
        {
            message.SendState = updateAction(message);
        }

    }

    public class SMSMessageGraph : Graph<SMSMessageDN, SMSMessageState>
    {
        public static void Register()
        {
            GetState = m => m.State;

            new ConstructFrom<SMSTemplateDN>(SMSMessageOperations.Create, SMSMessageState.Created)
            {
                CanConstruct = t => !t.Active ? "The template must be Active to allow constructing SMS messages" : null,
                Construct = (t, args) =>
                {
                    var message = t.CreateSMSMessage();
                    message.DestinationNumber = args.GetArg<string>(0);
                    return t.CreateSMSMessage();
                }
            }.Register();

            new Goto(SMSMessageOperations.Send, SMSMessageState.Sent)
            {
                FromStates = new[] { SMSMessageState.Created },
                Execute = (t, args) => { SMSLogic.SendSMS(t, args.TryGetArgC<Func<SMSMessageDN, string>>(0)); }
            }.Register();

            new Goto(SMSMessageOperations.UpdateStatus, SMSMessageState.Sent)
            {
                FromStates = new[] { SMSMessageState.Sent },
                Execute = (t, _) => { SMSLogic.UpdateMessageStatus(t, _.TryGetArgC<Func<SMSMessageDN, SendState>>(0)); }
            }.Register();
        }
    }

}
