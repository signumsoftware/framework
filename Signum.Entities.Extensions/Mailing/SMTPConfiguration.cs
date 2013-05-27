using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Main)]
    public class SMTPConfigurationDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        int port = 25;
        public int Port
        {
            get { return port; }
            set { Set(ref port, value, () => Port); }
        }

        string host;
        public string Host
        {
            get { return host; }
            set { Set(ref host, value, () => Host); }
        }

        bool useDefaultCredentials = true;
        public bool UseDefaultCredentials
        {
            get { return useDefaultCredentials; }
            set { Set(ref useDefaultCredentials, value, () => UseDefaultCredentials); }
        }

        string username;
        public string Username
        {
            get { return username; }
            set { Set(ref username, value, () => Username); }
        }

        string password;
        public string Password
        {
            get { return password; }
            set { Set(ref password, value, () => Password); }
        }

        [NotNullable]
        EmailContactDN defaultFrom;
        [NotNullValidator]
        public EmailContactDN DefaultFrom
        {
            get { return defaultFrom; }
            set { Set(ref defaultFrom, value, () => DefaultFrom); }
        }

        [NotNullable]
        MList<EmailRecipientDN> aditionalRecipients = new MList<EmailRecipientDN>();
        [NotNullValidator, NoRepeatValidator]
        public MList<EmailRecipientDN> AditionalRecipients
        {
            get { return aditionalRecipients; }
            set { Set(ref aditionalRecipients, value, () => AditionalRecipients); }
        }


        bool enableSSL;
        public bool EnableSSL
        {
            get { return enableSSL; }
            set { Set(ref enableSSL, value, () => EnableSSL); }
        }

        [NotNullable]
        MList<ClientCertificationFileDN> clientCertificationFiles = new MList<ClientCertificationFileDN>();
        public MList<ClientCertificationFileDN> ClientCertificationFiles
        {
            get { return clientCertificationFiles; }
            set { Set(ref clientCertificationFiles, value, () => ClientCertificationFiles); }
        }

        static readonly Expression<Func<SMTPConfigurationDN, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }


    public enum SMTPConfigurationOperation
    {
        Save
    }

    [Serializable, EntityKind(EntityKind.System)]
    public class ClientCertificationFileDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        string fullFilePath;
        public string FullFilePath
        {
            get { return fullFilePath; }
            set { Set(ref fullFilePath, value, () => FullFilePath); }
        }

        CertFileType certFileType;
        public CertFileType CertFileType
        {
            get { return certFileType; }
            set { Set(ref certFileType, value, () => CertFileType); }
        }

        static readonly Expression<Func<ClientCertificationFileDN, string>> ToStringExpression = e => e.name;
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
