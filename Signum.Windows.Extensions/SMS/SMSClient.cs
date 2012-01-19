using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Entities.SMS;

namespace Signum.Windows.SMS
{
    public static class SMSClient
    {
        public static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(ReflectionTools.GetMethodInfo(() => SMSClient.Start()));
        }

       
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<SMSMessageDN>(EntityType.NotSaving) { View = e => new SMSMessage() },
                    new EntitySettings<SMSTemplateDN>(EntityType.NotSaving) { View = e => new SMSTemplate() },
                    new EntitySettings<SMSSendPackageDN>(EntityType.NotSaving) { View = e => new SMSSendPackage()},
                    new EntitySettings<SMSUpdatePackageDN>(EntityType.NotSaving) { View = e => new SMSUpdatePackage()},
                });
            }
        }
    }
}
