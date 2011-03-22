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

namespace Signum.Engine.SMS
{
    public static class SMSLogic
    {
        private static Action<SMSMessageDN> SMSSendAction;
        public static bool IsStarted;

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
                                                        Active = t.Active(),
                                                        Message = t.Message.Etc(20),
                                                        Source = t.From,
                                                        t.State,
                                                        t.StartDate,
                                                        t.EndDate,
                                                     }).ToDynamic();

                IsStarted = true;
            }
        }

        public static void RegisterSMSSendAction(Action<SMSMessageDN> action)
        {
            SMSSendAction = action;
        }

        public static void SendSMS(SMSMessageDN message)
        {
            if (SMSSendAction == null)
                throw new InvalidOperationException(Resources.SMSSendActionWasNotEstablished);
            SMSSendAction(message);
        }
    }
}
