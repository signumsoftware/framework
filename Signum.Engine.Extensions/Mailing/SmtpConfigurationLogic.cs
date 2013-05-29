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
    public static class SMTPConfigurationLogic
    {
        public static ResetLazy<Dictionary<Lite<SMTPConfigurationDN>, SMTPConfigurationDN>> SmtpConfigCache; 

        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => SMTPConfigurationLogic.Start(null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SMTPConfigurationDN>();

                dqm.RegisterQuery(typeof(SMTPConfigurationDN), () =>
                    from s in Database.Query<SMTPConfigurationDN>()
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

                dqm.RegisterQuery(typeof(ClientCertificationFileDN), () =>
                    from c in Database.Query<ClientCertificationFileDN>()
                    select new
                    {
                        Entity = c,
                        c.Id,
                        c.Name,
                        CertFileType = c.CertFileType.NiceToString(),
                        c.FullFilePath
                    });

                SmtpConfigCache = sb.GlobalLazy(() => Database.Query<SMTPConfigurationDN>().ToDictionary(a => a.ToLite()),
                    new InvalidateWith(typeof(SMTPConfigurationDN)));

                new Graph<SMTPConfigurationDN>.Execute(SMTPConfigurationOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (sc, _) => { },
                }.Register();
            }
        }


        public static SmtpClient GenerateSmtpClient(this Lite<SMTPConfigurationDN> config)
        {
            return config.RetrieveFromCache().GenerateSmtpClient();
        }

        public static SMTPConfigurationDN RetrieveFromCache(this Lite<SMTPConfigurationDN> config)
        {
            return SmtpConfigCache.Value.GetOrThrow(config);
        }

        public static SmtpClient GenerateSmtpClient(this SMTPConfigurationDN config)
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
