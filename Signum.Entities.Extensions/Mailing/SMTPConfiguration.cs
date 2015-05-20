using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Files;
using System.Net.Mail;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class SmtpConfigurationEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        SmtpDeliveryFormat deliveryFormat;
        public SmtpDeliveryFormat DeliveryFormat
        {
            get { return deliveryFormat; }
            set { Set(ref deliveryFormat, value); }
        }

        SmtpDeliveryMethod deliveryMethod;
        public SmtpDeliveryMethod DeliveryMethod
        {
            get { return deliveryMethod; }
            set { Set(ref deliveryMethod, value); }
        }

        SmtpNetworkDeliveryEntity network;
        public SmtpNetworkDeliveryEntity Network
        {
            get { return network; }
            set { Set(ref network, value); }
        }

        [SqlDbType(Size = 300)]
        string pickupDirectoryLocation;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 300), FileNameValidator]
        public string PickupDirectoryLocation
        {
            get { return pickupDirectoryLocation; }
            set { Set(ref pickupDirectoryLocation, value); }
        }

        EmailAddressEntity defaultFrom;
        public EmailAddressEntity DefaultFrom
        {
            get { return defaultFrom; }
            set { Set(ref defaultFrom, value); }
        }

        [NotNullable]
        MList<EmailRecipientEntity> additionalRecipients = new MList<EmailRecipientEntity>();
        [NoRepeatValidator]
        public MList<EmailRecipientEntity> AdditionalRecipients
        {
            get { return additionalRecipients; }
            set { Set(ref additionalRecipients, value); }
        }

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

        static readonly Expression<Func<SmtpConfigurationEntity, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }


    public static class SmtpConfigurationOperation
    {
        public static readonly ExecuteSymbol<SmtpConfigurationEntity> Save = OperationSymbol.Execute<SmtpConfigurationEntity>();
    }

    [Serializable]
    public class SmtpNetworkDeliveryEntity : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        string host;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Host
        {
            get { return host; }
            set { Set(ref host, value); }
        }

        int port = 25;
        public int Port
        {
            get { return port; }
            set { Set(ref port, value); }
        }

        [SqlDbType(Size = 100)]
        string username;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string Username
        {
            get { return username; }
            set { Set(ref username, value); }
        }

        [SqlDbType(Size = 100)]
        string password;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string Password
        {
            get { return password; }
            set { Set(ref password, value); }
        }

        bool useDefaultCredentials = true;
        public bool UseDefaultCredentials
        {
            get { return useDefaultCredentials; }
            set { Set(ref useDefaultCredentials, value); }
        }

        bool enableSSL;
        public bool EnableSSL
        {
            get { return enableSSL; }
            set { Set(ref enableSSL, value); }
        }

        [NotNullable]
        MList<ClientCertificationFileEntity> clientCertificationFiles = new MList<ClientCertificationFileEntity>();
        public MList<ClientCertificationFileEntity> ClientCertificationFiles
        {
            get { return clientCertificationFiles; }
            set { Set(ref clientCertificationFiles, value); }
        }
    }

    [Serializable]
    public class ClientCertificationFileEntity : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = 300)]
        string fullFilePath;
        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 300),]
        public string FullFilePath
        {
            get { return fullFilePath; }
            set { Set(ref fullFilePath, value); }
        }

        CertFileType certFileType;
        public CertFileType CertFileType
        {
            get { return certFileType; }
            set { Set(ref certFileType, value); }
        }

        static readonly Expression<Func<ClientCertificationFileEntity, string>> ToStringExpression = e => e.fullFilePath;
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
