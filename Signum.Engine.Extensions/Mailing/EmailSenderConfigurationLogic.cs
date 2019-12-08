using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Maps;
using System.Reflection;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Mailing;
using Signum.Entities;
using Signum.Utilities;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Signum.Engine.Operations;

namespace Signum.Engine.Mailing
{
    public static class EmailSenderConfigurationLogic
    {
        public static ResetLazy<Dictionary<Lite<EmailSenderConfigurationEntity>, EmailSenderConfigurationEntity>> SmtpConfigCache = null!;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailSenderConfigurationEntity>()
                    .WithQuery(() => s => new
                    {
                        Entity = s,
                        s.Id,
                        s.Name,
                        s.SMTP!.DeliveryMethod,
                        s.SMTP!.Network!.Host,
                        s.SMTP!.PickupDirectoryLocation,
                        s.Exchange!.ExchangeVersion,
                        s.Exchange!.Url,
                    });
                
                SmtpConfigCache = sb.GlobalLazy(() => Database.Query<EmailSenderConfigurationEntity>().ToDictionary(a => a.ToLite()),
                    new InvalidateWith(typeof(EmailSenderConfigurationEntity)));

                new Graph<EmailSenderConfigurationEntity>.Execute(EmailSenderConfigurationOperation.Save)
                {
                    CanBeNew = true,
                    CanBeModified = true,
                    Execute = (sc, _) => { },
                }.Register();
            }
        }

        public static SmtpClient GenerateSmtpClient(this Lite<EmailSenderConfigurationEntity> config)
        {
            return config.RetrieveFromCache().SMTP.ThrowIfNull("No SMTP config").GenerateSmtpClient();
        }

        public static EmailSenderConfigurationEntity RetrieveFromCache(this Lite<EmailSenderConfigurationEntity> config)
        {
            return SmtpConfigCache.Value.GetOrThrow(config);
        }

        public static SmtpClient GenerateSmtpClient(this SmtpEmbedded config)
        {
            if (config.DeliveryMethod != SmtpDeliveryMethod.Network)
            {
                return new SmtpClient
                {
                    DeliveryFormat = config.DeliveryFormat,
                    DeliveryMethod = config.DeliveryMethod,
                    PickupDirectoryLocation = config.PickupDirectoryLocation,
                };
            }
            else
            {
                SmtpClient client = EmailLogic.SafeSmtpClient(config.Network!.Host, config.Network.Port);
                client.DeliveryFormat = config.DeliveryFormat;
                client.UseDefaultCredentials = config.Network.UseDefaultCredentials;
                client.Credentials = config.Network.Username.HasText() ? new NetworkCredential(config.Network.Username, config.Network.Password) : null;
                client.EnableSsl = config.Network.EnableSSL;

                foreach (var cc in config.Network.ClientCertificationFiles)
                {
                    client.ClientCertificates.Add(cc.CertFileType == CertFileType.CertFile ?
                        X509Certificate.CreateFromCertFile(cc.FullFilePath)
                        : X509Certificate.CreateFromSignedFile(cc.FullFilePath));
                }

                return client;
            }
        }
    }
}
