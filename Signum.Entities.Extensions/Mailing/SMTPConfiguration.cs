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
    public class SmtpConfigurationDN : Entity
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

        EmailAddressDN defaultFrom;
        public EmailAddressDN DefaultFrom
        {
            get { return defaultFrom; }
            set { Set(ref defaultFrom, value); }
        }

        [NotNullable]
        MList<EmailRecipientDN> aditionalRecipients = new MList<EmailRecipientDN>();
        [NoRepeatValidator]
        public MList<EmailRecipientDN> AditionalRecipients
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
        MList<ClientCertificationFileDN> clientCertificationFiles = new MList<ClientCertificationFileDN>();
        public MList<ClientCertificationFileDN> ClientCertificationFiles
        {
            get { return clientCertificationFiles; }
            set { Set(ref clientCertificationFiles, value); }
        }

        static readonly Expression<Func<SmtpConfigurationDN, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }


    public static class SmtpConfigurationOperation
    {
        public static readonly ExecuteSymbol<SmtpConfigurationDN> Save = OperationSymbol.Execute<SmtpConfigurationDN>();
    }

    [Serializable]
    public class ClientCertificationFileDN : EmbeddedEntity
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

        static readonly Expression<Func<ClientCertificationFileDN, string>> ToStringExpression = e => e.fullFilePath;
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
