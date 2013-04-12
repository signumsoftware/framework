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
        internal static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => SMTPConfigurationLogic.Start(null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SMTPConfigurationDN>();
                sb.Schema.EntityEvents<SMTPConfigurationDN>().Saving += new SavingEventHandler<SMTPConfigurationDN>(EmailClientSettingsLogic_Saving);

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

                sb.Schema.Initializing[InitLevel.Level2NormalEntities] += SetCache;

                new BasicExecute<SMTPConfigurationDN>(SMTPConfigurationOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (sc, _) => { },
                }.Register();
            }
        }

        static void EmailClientSettingsLogic_Saving(SMTPConfigurationDN ident)
        {
            if (ident.IsGraphModified)
                Transaction.PostRealCommit += ud => smtpConfigurations = null;
        }

        static void SetCache()
        {
            smtpConfigurations = Database.RetrieveAll<SMTPConfigurationDN>().ToDictionary(s => s.Name);
        }

        static Dictionary<string, SMTPConfigurationDN> smtpConfigurations;
        public static Dictionary<string, SMTPConfigurationDN> SmtpConfigurations
        {
            get
            {
                if (smtpConfigurations == null)
                    SetCache();
                return SMTPConfigurationLogic.smtpConfigurations;
            }
        }

        public static SmtpClient GenerateSmtpClient(string smtpSettingsName, bool defaultIfNotPresent)
        {
            var settings = SmtpConfigurations.TryGet(smtpSettingsName, null);
            if (settings == null)
                if (defaultIfNotPresent)
                    return EmailLogic.SafeSmtpClient();
                else
                    throw new ArgumentException("The setting {0} was not found in the SMTP settings cache".Formato(smtpSettingsName));

            SmtpClient client = EmailLogic.SafeSmtpClient(settings.Host, settings.Port);

            client.UseDefaultCredentials = settings.UseDefaultCredentials;
            client.Credentials = settings.Username.HasText() ? new NetworkCredential(settings.Username, settings.Password) : null;
            client.EnableSsl = settings.EnableSSL;

            foreach (var cc in settings.ClientCertificationFiles)
            {
                client.ClientCertificates.Add(cc.CertFileType == CertFileType.CertFile ?
                    X509Certificate.CreateFromCertFile(cc.FullFilePath)
                    : X509Certificate.CreateFromSignedFile(cc.FullFilePath));
            }

            return client;
        }

        public static SmtpClient GenerateSmtpClient(this Lite<SMTPConfigurationDN> config)
        {
            return GenerateSmtpClient(config.ToString(), false);
        }

        public static SmtpClient GenerateSmtpClient(this Lite<SMTPConfigurationDN> config, bool defaultIfNotPresent)
        {
            return GenerateSmtpClient(config.TryCC(c => c.ToString()), defaultIfNotPresent);
        }
    }
}
