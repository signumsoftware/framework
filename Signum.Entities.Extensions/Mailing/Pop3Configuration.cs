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
    public class Pop3ConfigurationEntity : Entity, ITaskEntity
    {
        bool active;
        public bool Active
        {
            get { return active; }
            set { Set(ref active, value); }
        }

        int port = 110;
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

        bool enableSSL;
        public bool EnableSSL
        {
            get { return enableSSL; }
            set
            {
                if (Set(ref enableSSL, value))
                {
                    Port = enableSSL ? 995 : 110;
                }
            }
        }

        int readTimeout = 60000;
        [NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, -1), Unit("ms")]
        public int ReadTimeout
        {
            get { return readTimeout; }
            set { Set(ref readTimeout, value); }
        }

        int? deleteMessagesAfter = 14;
        [Unit("d")]
        public int? DeleteMessagesAfter
        {
            get { return deleteMessagesAfter; }
            set { Set(ref deleteMessagesAfter, value); }
        }

        [NotNullable]
        MList<ClientCertificationFileEntity> clientCertificationFiles = new MList<ClientCertificationFileEntity>();
        public MList<ClientCertificationFileEntity> ClientCertificationFiles
        {
            get { return clientCertificationFiles; }
            set { Set(ref clientCertificationFiles, value); }
        }

        public override string ToString()
        {
            return "{0} ({1})".FormatWith(Username, Host);
        }

    }

    public static class Pop3ConfigurationOperation
    {
        public static readonly ExecuteSymbol<Pop3ConfigurationEntity> Save = OperationSymbol.Execute<Pop3ConfigurationEntity>();
        public static readonly ConstructSymbol<Pop3ReceptionEntity>.From<Pop3ConfigurationEntity> ReceiveEmails = OperationSymbol.Construct<Pop3ReceptionEntity>.From<Pop3ConfigurationEntity>();
    }

    public static class Pop3ConfigurationAction
    {
        public static SimpleTaskSymbol ReceiveAllActivePop3Configurations = new SimpleTaskSymbol(); 
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class Pop3ReceptionEntity : Entity
    {
        [NotNullable]
        Lite<Pop3ConfigurationEntity> pop3Configuration;
        [NotNullValidator]
        public Lite<Pop3ConfigurationEntity> Pop3Configuration
        {
            get { return pop3Configuration; }
            set { Set(ref pop3Configuration, value); }
        }

        DateTime startDate;
        public DateTime StartDate
        {
            get { return startDate; }
            set { Set(ref startDate, value); }
        }

        DateTime? endDate;
        public DateTime? EndDate
        {
            get { return endDate; }
            set { Set(ref endDate, value); }
        }

        int newEmails;
        public int NewEmails
        {
            get { return newEmails; }
            set { Set(ref newEmails, value); }
        }

        Lite<ExceptionEntity> exception;
        public Lite<ExceptionEntity> Exception
        {
            get { return exception; }
            set { Set(ref exception, value); }
        }
    }


    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class Pop3ReceptionExceptionEntity : Entity
    {
        [NotNullable]
        Lite<Pop3ReceptionEntity> reception;
        [NotNullValidator]
        public Lite<Pop3ReceptionEntity> Reception
        {
            get { return reception; }
            set { Set(ref reception, value); }
        }

        [NotNullable, UniqueIndex]
        Lite<ExceptionEntity> exception;
        [NotNullValidator]
        public Lite<ExceptionEntity> Exception
        {
            get { return exception; }
            set { Set(ref exception, value); }
        }
    }
}
