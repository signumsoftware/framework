using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Processes;
using Signum.Entities.Mailing;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Linq.Expressions;

namespace Signum.Entities.Mailing
{
    [Serializable]
    public class NewsletterDN : Entity, IProcessDataDN
    {
        [NotNullable, SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        NewsletterState state = NewsletterState.Created;
        public NewsletterState State
        {
            get { return state; }
            set { Set(ref state, value, () => State); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string htmlBody;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = int.MaxValue)]
        public string HtmlBody
        {
            get { return htmlBody; }
            set { Set(ref htmlBody, value, () => HtmlBody); }
        }

        [NotNullable, SqlDbType(Size = 50)]
        string subject;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 50)]
        public string Subject
        {
            get { return subject; }
            set { Set(ref subject, value, () => Subject); }
        }

        Lite<SMTPConfigurationDN> smtpConfig = DefaultSMTPConfig;
        public Lite<SMTPConfigurationDN> SMTPConfig
        {
            get { return smtpConfig; }
            set { Set(ref smtpConfig, value, () => SMTPConfig); }
        }

        [NotNullable, SqlDbType(Size = 50)]
        string from = DefaultFrom;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 50)]
        public string From
        {
            get { return from; }
            set { Set(ref from, value, () => From); }
        }

        [NotNullable, SqlDbType(Size = 50)]
        string displayFrom = DefaultDisplayFrom;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 50)]
        public string DiplayFrom
        {
            get { return displayFrom; }
            set { Set(ref displayFrom, value, () => DiplayFrom); }
        }

        static readonly Expression<Func<NewsletterDN, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public static Lite<SMTPConfigurationDN> DefaultSMTPConfig;
        public static string DefaultFrom;
        public static string DefaultDisplayFrom;

        QueryDN query;
        public QueryDN Query
        {
            get { return query; }
            set { Set(ref query, value, () => Query); }
        }
    }

    [Serializable]
    public class NewsletterDeliveryDN : Entity
    {
        bool sent;
        public bool Sent
        {
            get { return sent; }
            set { Set(ref sent, value, () => Sent); }
        }

        DateTime? sendDate;
        [DateTimePrecissionValidator(DateTimePrecision.Seconds)]
        public DateTime? SendDate
        {
            get { return sendDate; }
            set { Set(ref sendDate, value, () => SendDate); }
        }

        Lite<IEmailOwnerDN> recipient;
        public Lite<IEmailOwnerDN> Recipient
        {
            get { return recipient; }
            set { Set(ref recipient, value, () => Recipient); }
        }

        Lite<NewsletterDN> newsletter;
        public Lite<NewsletterDN> Newsletter
        {
            get { return newsletter; }
            set { Set(ref newsletter, value, () => Newsletter); }
        }

        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }
    }


    public enum NewsletterOperations
    {
        Save,
        Send,
        AddRecipients,
        RemoveRecipients,
        Clone,
    }

    public enum NewsletterState
    { 
        Created,
        Saved,
        Sent
    }
}
