using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Files;

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

        int port = 25;
        public int Port
        {
            get { return port; }
            set { Set(ref port, value); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string host;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Host
        {
            get { return host; }
            set { Set(ref host, value); }
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

        EmailAddressEntity defaultFrom;
        public EmailAddressEntity DefaultFrom
        {
            get { return defaultFrom; }
            set { Set(ref defaultFrom, value); }
        }

        [NotNullable]
        MList<EmailRecipientEntity> aditionalRecipients = new MList<EmailRecipientEntity>();
        [NoRepeatValidator]
        public MList<EmailRecipientEntity> AditionalRecipients
        {
            get { return aditionalRecipients; }
            set { Set(ref aditionalRecipients, value); }
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
    public class ClientCertificationFileEntity : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = 300)]
        string fullFilePath;
        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 300), ]
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
