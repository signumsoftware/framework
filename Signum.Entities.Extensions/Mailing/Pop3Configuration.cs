using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities.Mailing;
using Signum.Entities.Scheduler;
using Signum.Utilities;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class Pop3ConfigurationDN : Entity, ITaskDN
    {
        bool active;
        public bool Active
        {
            get { return active; }
            set { Set(ref active, value, () => Active); }
        }

        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        int port = 110;
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

        bool enableSSL;
        public bool EnableSSL
        {
            get { return enableSSL; }
            set
            {
                if (Set(ref enableSSL, value, () => EnableSSL))
                {
                    Port = enableSSL ? 995 : 110;
                }
            }
        }

        int readTimeout = 60000;
        [NumberIsValidator(ComparisonType.GreaterThanOrEqual, -1), Unit("ms")]
        public int ReadTimeout
        {
            get { return readTimeout; }
            set { Set(ref readTimeout, value, () => ReadTimeout); }
        }

        int maxDownloadEmails = 100;
        public int MaxDownloadEmails
        {
            get { return maxDownloadEmails; }
            set { Set(ref maxDownloadEmails, value, () => MaxDownloadEmails); }
        }

        int? deleteMessagesAfter = 14;
        [Unit("d")]
        public int? DeleteMessagesAfter
        {
            get { return deleteMessagesAfter; }
            set { Set(ref deleteMessagesAfter, value, () => DeleteMessagesAfter); }
        }

        [NotNullable]
        MList<ClientCertificationFileDN> clientCertificationFiles = new MList<ClientCertificationFileDN>();
        public MList<ClientCertificationFileDN> ClientCertificationFiles
        {
            get { return clientCertificationFiles; }
            set { Set(ref clientCertificationFiles, value, () => ClientCertificationFiles); }
        }

        static Expression<Func<Pop3ConfigurationDN, string>> ToStringExpression = e => e.Name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

    }

    public enum Pop3ConfigurationOperation
    {
        Save,
        ReceiveEmails
    }

    public enum Pop3ConfigurationAction
    {
        ReceiveAllActivePop3Configurations
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class Pop3ReceptionDN : Entity
    {
        [NotNullable]
        Lite<Pop3ConfigurationDN> pop3Configuration;
        [NotNullValidator]
        public Lite<Pop3ConfigurationDN> Pop3Configuration
        {
            get { return pop3Configuration; }
            set { Set(ref pop3Configuration, value, () => Pop3Configuration); }
        }

        DateTime startDate;
        public DateTime StartDate
        {
            get { return startDate; }
            set { Set(ref startDate, value, () => StartDate); }
        }

        DateTime? endDate;
        public DateTime? EndDate
        {
            get { return endDate; }
            set { Set(ref endDate, value, () => EndDate); }
        }

        int numberOfMails;
        public int NumberOfMails
        {
            get { return numberOfMails; }
            set { Set(ref numberOfMails, value, () => NumberOfMails); }
        }

        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }
    }
}
