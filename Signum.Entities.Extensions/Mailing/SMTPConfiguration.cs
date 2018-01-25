using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Files;
using System.Net.Mail;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class SmtpConfigurationEntity : Entity
    {
        static SmtpConfigurationEntity()
        {
            DescriptionManager.ExternalEnums.Add(typeof(SmtpDeliveryFormat), m => m.Name);
            DescriptionManager.ExternalEnums.Add(typeof(SmtpDeliveryMethod), m => m.Name);
        }

        [UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string Name { get; set; }

        public SmtpDeliveryFormat DeliveryFormat { get; set; }

        public SmtpDeliveryMethod DeliveryMethod { get; set; }

        public SmtpNetworkDeliveryEmbedded Network { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 300), FileNameValidator]
        public string PickupDirectoryLocation { get; set; }

        public EmailAddressEmbedded DefaultFrom { get; set; }

        [NotNullValidator]
        [NoRepeatValidator]
        public MList<EmailRecipientEntity> AdditionalRecipients { get; set; } = new MList<EmailRecipientEntity>();

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            return stateValidator.Validate(this, pi) ?? base.PropertyValidation(pi);
        }

        static StateValidator<SmtpConfigurationEntity, SmtpDeliveryMethod> stateValidator = new StateValidator<SmtpConfigurationEntity, SmtpDeliveryMethod>(
            a => a.DeliveryMethod, a => a.Network, a => a.PickupDirectoryLocation)
            {
                {SmtpDeliveryMethod.Network,        true, null },
                {SmtpDeliveryMethod.SpecifiedPickupDirectory, null, true},
                {SmtpDeliveryMethod.PickupDirectoryFromIis,    null, null },
            };

        static Expression<Func<SmtpConfigurationEntity, string>> ToStringExpression = e => e.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }


    [AutoInit]
    public static class SmtpConfigurationOperation
    {
        public static ExecuteSymbol<SmtpConfigurationEntity> Save;
    }

    [Serializable]
    public class SmtpNetworkDeliveryEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Host { get; set; }

        public int Port { get; set; } = 25;

        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string Username { get; set; }

        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string Password { get; set; }

        public bool UseDefaultCredentials { get; set; } = true;

        public bool EnableSSL { get; set; }

        [NotNullValidator]
        public MList<ClientCertificationFileEmbedded> ClientCertificationFiles { get; set; } = new MList<ClientCertificationFileEmbedded>();
    }

    [Serializable]
    public class ClientCertificationFileEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 300),]
        public string FullFilePath { get; set; }

        public CertFileType CertFileType { get; set; }

        static Expression<Func<ClientCertificationFileEmbedded, string>> ToStringExpression = e => e.FullFilePath;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public enum CertFileType
    {
        CertFile,
        SignedFile
    }
}
