using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Entities.Processes;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Mailing;
using Signum.Entities.Basics;

namespace Signum.Entities.Mailing
{
    [Serializable]
    public class EmailMessageDN : Entity
    {
        [ImplementedBy(typeof(UserDN))]
        Lite<IEmailOwnerDN> recipient;
        [NotNullValidator]
        public Lite<IEmailOwnerDN> Recipient
        {
            get { return recipient; }
            set { Set(ref recipient, value, () => Recipient); }
        }

        string bcc;
        public string Bcc
        {
            get { return bcc; }
            set { Set(ref bcc, value, () => Bcc); }
        }

        string cc;
        public string Cc
        {
            get { return cc; }
            set { Set(ref cc, value, () => Cc); }
        }

        Lite<EmailTemplateDN> template;
        [NotNullValidator]
        public Lite<EmailTemplateDN> Template
        {
            get { return template; }
            set { Set(ref template, value, () => Template); }
        }

        DateTime creationTime = TimeZoneManager.Now;
        public DateTime CreationTime
        {
            get { return creationTime; }
            private set { Set(ref creationTime, value, () => CreationTime); }
        }

        DateTime? sent;
        public DateTime? Sent
        {
            get { return sent; }
            set { SetToStr(ref sent, value, () => Sent); }
        }

        DateTime? received;
        public DateTime? Received
        {
            get { return received; }
            set { Set(ref received, value, () => Received); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string subject;
        [StringLengthValidator(AllowNulls = false, Min = 3)]
        public string Subject
        {
            get { return subject; }
            set { Set(ref subject, value, () => Subject); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string body;
        [StringLengthValidator(AllowNulls = false, Min = 3)]
        public string Body
        {
            get { return body; }
            set { Set(ref body, value, () => Body); }
        }

        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }

        EmailState state;
        public EmailState State
        {
            get { return state; }
            set { Set(ref state, value, () => State); }
        }

        Lite<EmailPackageDN> package;
        public Lite<EmailPackageDN> Package
        {
            get { return package; }
            set { Set(ref package, value, () => Package); }
        }

        static StateValidator<EmailMessageDN, EmailState> validator = new StateValidator<EmailMessageDN, EmailState>(
            m => m.State, m => m.Exception, m => m.Sent, m => m.Received, m => m.Package)
            {
{EmailState.Created,      false,             false,      false,         null },
{EmailState.Sent,         false,             true,       false,         null },
{EmailState.Exception,    true,              true,       false,         null },
{EmailState.Received,     false,             true,       true,          null },
            };
    }

    public enum EmailState
    {
        Created,
        Sent,
        Exception,
        Received
    }

    public interface IEmailOwnerDN : IIdentifiable
    {
        string Email { get; }
        string CultureInfo { get; }
    }

    public enum EmailProcesses
    {
        SendEmails
    }

    public enum EmailOperations
    {
        ReSendEmails
    }

    [Serializable]
    public class EmailPackageDN : IdentifiableEntity, IProcessDataDN
    {
        [SqlDbType(Size = 200)]
        string name;
        [StringLengthValidator(AllowNulls = true, Max = 200)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        public override string ToString()
        {
            return "EmailPackage {0}".Formato(Name);
        }
    }
}

