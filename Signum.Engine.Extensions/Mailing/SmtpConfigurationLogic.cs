using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
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
    public static class SmtpConfigurationLogic
    {
        public static ResetLazy<Dictionary<Lite<SmtpConfigurationEntity>, SmtpConfigurationEntity>> SmtpConfigCache;
        public static Func<SmtpConfigurationEntity> DefaultSmtpConfiguration;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, Func<SmtpConfigurationEntity> defaultSmtpConfiguration)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SmtpConfigurationEntity>();

                DefaultSmtpConfiguration = defaultSmtpConfiguration;

                dqm.RegisterQuery(typeof(SmtpConfigurationEntity), () =>
                    from s in Database.Query<SmtpConfigurationEntity>()
                    select new
                    {
                        Entity = s,
                        s.Id,
                        s.Name,
                        s.Host,
                        s.Port,
                        s.UseDefaultCredentials,
                        s.Username,
                        s.Password,
                        s.EnableSSL
                    });

                SmtpConfigCache = sb.GlobalLazy(() => Database.Query<SmtpConfigurationEntity>().ToDictionary(a => a.ToLite()),
                    new InvalidateWith(typeof(SmtpConfigurationEntity)));

                new Graph<SmtpConfigurationEntity>.Execute(SmtpConfigurationOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (sc, _) => { },
                }.Register();
            }
        }

        public static SmtpClient GenerateSmtpClient(this Lite<SmtpConfigurationEntity> config)
        {
            return config.RetrieveFromCache().GenerateSmtpClient();
        }

        public static SmtpConfigurationEntity RetrieveFromCache(this Lite<SmtpConfigurationEntity> config)
        {
            return SmtpConfigCache.Value.GetOrThrow(config);
        }

        public static SmtpClient GenerateSmtpClient(this SmtpConfigurationEntity config)
        {
            SmtpClient client = EmailLogic.SafeSmtpClient(config.Host, config.Port);

            client.UseDefaultCredentials = config.UseDefaultCredentials;
            client.Credentials = config.Username.HasText() ? new NetworkCredential(config.Username, config.Password) : null;
            client.EnableSsl = config.EnableSSL;

            foreach (var cc in config.ClientCertificationFiles)
            {
                client.ClientCertificates.Add(cc.CertFileType == CertFileType.CertFile ?
                    X509Certificate.CreateFromCertFile(cc.FullFilePath)
                    : X509Certificate.CreateFromSignedFile(cc.FullFilePath));
            }

            return client;
        }
    }
}
