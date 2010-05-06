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

        EmailTemplateDN template;
        [NotNullValidator]
        public EmailTemplateDN Template
        {
            get { return template; }
            set { Set(ref template, value, () => Template); }
        }

        DateTime sent;
        public DateTime Sent
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

        [NotNullable, SqlDbType(Size = 100)]
        string subject;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Subject
        {
            get { return subject; }
            set { Set(ref subject, value, () => Subject); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
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

        static StateValidator<EmailMessageDN, EmailState> validator = new StateValidator<EmailMessageDN,EmailState>(
            m => m.State, m => m.Body, m=> m.Subject, m=>m.Exception, m=> m.Sent, m=>m.Received, m=>m.Package)
            {
{EmailState.Prepared,    false,      false,          false,      false,          false,          true },
{EmailState.SentOk,      true,       true,           false,      true,           false,          null },
{EmailState.SentError,   true,       true,           true,       true,           null,           null },
{EmailState.Received,    true,       true,           false,      true,           true,           null },
            };
        }
    }

    public enum EmailState
    {
        Prepared,
        SentOk,
        SentError,
        Received
    }

    [ImplementedBy(typeof(UserDN))]
    public interface IEmailOwnerDN : IIdentifiable
    {
        string Email { get; }
    }

    public enum EmailOperation
    {
        SendNewsletter,
        SendEmailConfirmation
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

        EmailTemplateDN template;
        public EmailTemplateDN Template
        {
            get { return template; }
            set { SetToStr(ref template, value, () => Template); }
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

        public override string ToString()
        {
            return "{0} {1} ({2} lines{3})".Formato(Template, Name, numLines, numErrors == 0 ? "" : ", {0} errors".Formato(numErrors));
        }
    }
