using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Entities.Mailing;
using System.Windows;

namespace Signum.Windows.Mailing
{
    public class MailingClient
    {
        public static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(ReflectionTools.GetMethodInfo(() => MailingClient.Start(true, true)));
        }

        public static void Start(bool smtp, bool pop3)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings> 
                { 
                    new EntitySettings<EmailMessageEntity> { View = e => new EmailMessage() },
                    new EmbeddedEntitySettings<EmailAttachmentEmbedded> { View = e => new EmailAttachment() },
                    new EmbeddedEntitySettings<EmailAddressEmbedded> { View = e => new EmailAddress() },
                    new EmbeddedEntitySettings<EmailRecipientEntity> { View = e => new EmailRecipient() }
                });

                if (smtp || pop3)
                    Navigator.AddSetting(new EmbeddedEntitySettings<ClientCertificationFileEmbedded> { View = e => new ClientCertificationFile() });

                if (smtp)
                {
                    Navigator.AddSettings(new List<EntitySettings>
                    {
                        new EntitySettings<SmtpConfigurationEntity> { View = e => new SmtpConfiguration() },
                        
                    });
                }

                if (pop3)
                    Navigator.AddSetting(new EntitySettings<Pop3ConfigurationEntity> { View = e => new Pop3Configuration() });
            }
        }
    }
}
