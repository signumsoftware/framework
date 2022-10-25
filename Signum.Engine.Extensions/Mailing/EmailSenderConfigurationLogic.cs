using Signum.Entities.Mailing;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Signum.Engine.Mailing;

public static class EmailSenderConfigurationLogic
{
    public static ResetLazy<Dictionary<Lite<EmailSenderConfigurationEntity>, EmailSenderConfigurationEntity>> SmtpConfigCache = null!;

    public static Func<string, string> EncryptPassword = s => s;
    public static Func<string, string> DecryptPassword = s => s;

    public static void Start(SchemaBuilder sb, Func<string, string>? encryptPassword = null, Func<string, string>? decryptPassword = null)
    {
        sb.Settings.AssertImplementedBy((EmailSenderConfigurationEntity o) => o.Service, typeof(SmtpEmailServiceEntity));
        sb.Settings.AssertImplementedBy((EmailSenderConfigurationEntity o) => o.Service, typeof(ExchangeWebServiceEmailServiceEntity));
        sb.Settings.AssertImplementedBy((EmailSenderConfigurationEntity o) => o.Service, typeof(MicrosoftGraphEmailServiceEntity));

        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            if (encryptPassword != null)
                EncryptPassword = encryptPassword;

            if (decryptPassword != null)
                DecryptPassword = decryptPassword;

            sb.Include<EmailSenderConfigurationEntity>()
                .WithDeletePart(a => a.Service, handleOnSaving: esc => true)
                .WithQuery(() => s => new
                {
                    Entity = s,
                    s.Id,
                    s.Name,
                    s.Service
                });
            
            SmtpConfigCache = sb.GlobalLazy(() => Database.Query<EmailSenderConfigurationEntity>().ToDictionary(a => a.ToLite()),
                new InvalidateWith(typeof(EmailSenderConfigurationEntity)));

            new Graph<EmailSenderConfigurationEntity>.Execute(EmailSenderConfigurationOperation.Save)
            {
                CanBeNew = true,
                CanBeModified = true,
                Execute = (sc, _) => { },
            }.Register();

            new Graph<EmailSenderConfigurationEntity>.ConstructFrom<EmailSenderConfigurationEntity>(EmailSenderConfigurationOperation.Clone)
            { 
                Construct = (sc, _) => sc.Clone()
            
            }.Register();

        }
    }

    public static SmtpClient GenerateSmtpClient(this Lite<EmailSenderConfigurationEntity> config)
    {
        return (config.RetrieveFromCache().Service as SmtpEmailServiceEntity).ThrowIfNull("No SMTP config").GenerateSmtpClient();
    }

    public static EmailSenderConfigurationEntity RetrieveFromCache(this Lite<EmailSenderConfigurationEntity> config)
    {
        return SmtpConfigCache.Value.GetOrThrow(config);
    }

    public static SmtpClient GenerateSmtpClient(this SmtpEmailServiceEntity config)
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
            client.Credentials = config.Network.Username.HasText() ? new NetworkCredential(config.Network.Username, DecryptPassword(config.Network.Password!)) : null;
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
