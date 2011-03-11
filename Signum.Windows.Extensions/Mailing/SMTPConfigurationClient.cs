using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Entities.Mailing;
using System.Windows;

namespace Signum.Windows.Extensions.Mailing
{
    public class SMTPConfigurationClient
    {
        internal static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(ReflectionTools.GetMethodInfo(() => SMTPConfigurationClient.Start()));
        }

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<SMTPConfigurationDN>(EntityType.Default) { View = e => new SMTPConfiguration() },
                    new EntitySettings<ClientCertificationFileDN>(EntityType.Default) { View = e => new ClientCertificationFile() },

                });
            }
        }
    }
}
