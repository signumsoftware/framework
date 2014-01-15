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
        public static ResetLazy<Dictionary<Lite<SmtpConfigurationDN>, SmtpConfigurationDN>> SmtpConfigCache;
        public static ResetLazy<SmtpConfigurationDN> DefaultSmtpConfiguration;  

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SmtpConfigurationDN>();

                sb.AddUniqueIndex<SmtpConfigurationDN>(c => new { c.IsDefault }, c => c.IsDefault);

                dqm.RegisterQuery(typeof(SmtpConfigurationDN), () =>
                    from s in Database.Query<SmtpConfigurationDN>()
                    select new
                    {
                        Entity = s,
                        s.Id,
                        s.Host,
                        s.Port,
                        s.UseDefaultCredentials,
                        s.Username,
                        s.Password,
                        s.EnableSSL
                    });

                SmtpConfigCache = sb.GlobalLazy(() => Database.Query<SmtpConfigurationDN>().ToDictionary(a => a.ToLite()),
                    new InvalidateWith(typeof(SmtpConfigurationDN)));

                DefaultSmtpConfiguration = sb.GlobalLazy(() => SmtpConfigCache.Value.Values.SingleOrDefaultEx(a => a.IsDefault),
                    new InvalidateWith(typeof(SmtpConfigurationDN)));

                new Graph<SmtpConfigurationDN>.Execute(SmtpConfigurationOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (sc, _) => { },
                }.Register();
            }
        }

        public static SmtpClient GenerateSmtpClient(this Lite<SmtpConfigurationDN> config)
        {
            return config.RetrieveFromCache().GenerateSmtpClient();
        }

        public static SmtpConfigurationDN RetrieveFromCache(this Lite<SmtpConfigurationDN> config)
        {
            return SmtpConfigCache.Value.GetOrThrow(config);
        }

        public static SmtpClient GenerateSmtpClient(this SmtpConfigurationDN config)
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
