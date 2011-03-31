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

namespace Signum.Engine.SMS
{
    public static class SMSLogic
    {
        private static Action<SMSMessageDN> SMSSendAction;

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null, null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, Action<SMSMessageDN> sendAction)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SMSMessageDN>();

                SMSSendAction = sendAction;
                
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
            }
        }

        public static void RegisterSMSSendAction(Action<SMSMessageDN> action)
        {
            SMSSendAction = action;
        }

        public static void SendSMS(SMSMessageDN message)
        {
            if (SMSSendAction == null)
                throw new InvalidOperationException("SMSSendAction was not established");
            SendSMS(message, SMSSendAction);
        }

        public static void SendSMS(SMSMessageDN message, Action<SMSMessageDN> send) //Allow various custom sendProviders
        {
            send(message);
            message.SendDate = DateTime.Now; 
            message.State = SMSMessageState.Sent;
            message.Save();
        }

    }
}
