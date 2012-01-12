using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Entities.Operations;
using Signum.Entities.Processes;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Mailing;

namespace Signum.Entities.Mailing
{
    [Serializable]
    public class EmailMessageDN : Entity
    {
        Lite<IEmailOwnerDN> recipient;
        [NotNullValidator]
        public Lite<IEmailOwnerDN> Recipient
        {
            get { return recipient; }
            set { Set(ref recipient, value, () => Recipient); }
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

        [SqlDbType(Size = 400)]
        string subject;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 400)]
        public string Subject
        {
            get { return subject; }
            set { Set(ref subject, value, () => Subject); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string body;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = int.MaxValue)]
        public string Body
        {
            get { return body; }
            set { Set(ref body, value, () => Body); }
        }

        string exception;
        [StringLengthValidator(AllowNulls = true, Max = int.MaxValue)]
        public string Exception
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
{EmailState.SentError,    true,              true,       false,         null },
{EmailState.Received,     false,             true,       true,          null },
            };
    }


    public enum EmailState
    {
        Created,
        Sent,
        SentError,
        Received
    }

    [ImplementedBy(typeof(UserDN))]
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

        Lite<UserDN> user;
        public Lite<UserDN> User
        {
            get { return user; }
            set { Set(ref user, value, () => User); }
        }

        int numLines;
        public int NumLines
        {
            get { return numLines; }
            set { SetToStr(ref numLines, value, () => NumLines); }
        }

        int numErrors;
        public int NumErrors
        {
            get { return numErrors; }
            set { SetToStr(ref numErrors, value, () => NumErrors); }
        }

        string overrideEmailAddress;
        public string OverrideEmailAddress
        {
            get { return overrideEmailAddress; }
            set { Set(ref overrideEmailAddress, value, () => OverrideEmailAddress); }
        }

        public override string ToString()
        {
            return "{0} ({1} lines{2})".Formato(Name, numLines, numErrors == 0 ? "" : ", {0} errors".Formato(numErrors));
        }
    }
}

