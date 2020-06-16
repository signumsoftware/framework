using System;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Net.Mail;
using Signum.Entities;
using Microsoft.Exchange.WebServices.Data;
using System.Reflection;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class EmailSenderConfigurationEntity : Entity
    {
        static EmailSenderConfigurationEntity()
        {
            DescriptionManager.ExternalEnums.Add(typeof(SmtpDeliveryFormat), m => m.Name);
            DescriptionManager.ExternalEnums.Add(typeof(SmtpDeliveryMethod), m => m.Name);
            DescriptionManager.ExternalEnums.Add(typeof(ExchangeVersion), m => m.Name);
        }

        [UniqueIndex]
        [StringLengthValidator(Min = 1, Max = 100)]
        public string Name { get; set; }

        public EmailAddressEmbedded? DefaultFrom { get; set; }

        [NoRepeatValidator]
        public MList<EmailRecipientEmbedded> AdditionalRecipients { get; set; } = new MList<EmailRecipientEmbedded>();


        public SmtpEmbedded? SMTP { get; set; }

        public ExchangeWebServiceEmbedded? Exchange { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);

        protected override string? PropertyValidation(PropertyInfo pi)
        {
            if(pi.Name == nameof(SMTP) || pi.Name == nameof(Exchange))
            {
                if (SMTP == null && Exchange == null)
                    return ValidationMessage._0Or1ShouldBeSet.NiceToString(
                        NicePropertyName(() => SMTP),
                        NicePropertyName(() => Exchange));


                if (SMTP != null && Exchange != null)
                    return ValidationMessage._0And1CanNotBeSetAtTheSameTime.NiceToString(
                        NicePropertyName(() => SMTP),
                        NicePropertyName(() => Exchange));
            }

            return base.PropertyValidation(pi);
        }
    }


    [AutoInit]
    public static class EmailSenderConfigurationOperation
    {
        public static ExecuteSymbol<EmailSenderConfigurationEntity> Save;
    }

    [Serializable]
    public class SmtpEmbedded : EmbeddedEntity
    {
        public SmtpDeliveryFormat DeliveryFormat { get; set; }

        public SmtpDeliveryMethod DeliveryMethod { get; set; }

        public SmtpNetworkDeliveryEmbedded? Network { get; set; }

        [StringLengthValidator(Min = 3, Max = 300), FileNameValidator]
        public string? PickupDirectoryLocation { get; set; }

        static StateValidator<SmtpEmbedded, SmtpDeliveryMethod> stateValidator = new StateValidator<SmtpEmbedded, SmtpDeliveryMethod>(
           a => a.DeliveryMethod, a => a.Network, a => a.PickupDirectoryLocation)
            {
                {SmtpDeliveryMethod.Network,        true, null },
                {SmtpDeliveryMethod.SpecifiedPickupDirectory, null, true},
                {SmtpDeliveryMethod.PickupDirectoryFromIis,    null, null },
            };

        protected override string? PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            return stateValidator.Validate(this, pi) ?? base.PropertyValidation(pi);
        }
    }

    [Serializable]
    public class SmtpNetworkDeliveryEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Host { get; set; }

        public int Port { get; set; } = 25;

        [StringLengthValidator(Max = 100)]
        public string? Username { get; set; }

        [StringLengthValidator(Max = 100)]
        public string? Password { get; set; }

        public bool UseDefaultCredentials { get; set; } = true;

        public bool EnableSSL { get; set; }

        
        public MList<ClientCertificationFileEmbedded> ClientCertificationFiles { get; set; } = new MList<ClientCertificationFileEmbedded>();
    }

    [Serializable]
    public class ClientCertificationFileEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(Min = 2, Max = 300),]
        public string FullFilePath { get; set; }

        public CertFileType CertFileType { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => FullFilePath);
    }

    public enum CertFileType
    {
        CertFile,
        SignedFile
    }

    [Serializable]
    public class ExchangeWebServiceEmbedded : EmbeddedEntity
    {
        public ExchangeVersion ExchangeVersion { get; set; }

        [StringLengthValidator(Max = 300)]
        public string? Url { get; set; }


        [StringLengthValidator(Max = 100)]
        public string? Username { get; set; }

        [StringLengthValidator(Max = 100)]
        public string? Password { get; set; }

        public bool UseDefaultCredentials { get; set; } = true;
    }
}
