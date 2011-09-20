using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Processes;
using Signum.Entities.Mailing;
using Signum.Utilities;

namespace Signum.Entities.Mailing
{
    [Serializable]
    public class NewsletterDN : Entity, IProcessDataDN
    {
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

        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        NewsletterState  state;
        public NewsletterState  State
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
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 50), EMailValidator]
        public string From
        {
            get { return from; }
            set { Set(ref from, value, () => From); }
        }

        public override string ToString()
        {
            return name;
        }

        public static Lite<SMTPConfigurationDN> DefaultSMTPConfig;
        public static string DefaultFrom;
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

        [SqlDbType(Size = int.MaxValue)]
        string error;
        [StringLengthValidator(AllowNulls = true, Max = int.MaxValue)]
        public string Error
        {
            get { return error; }
            set { Set(ref error, value, () => Error); }
        }
    }


    public enum NewsletterOperations
    { 
        Save,
        Send
    }

    public enum NewsletterState
    { 
        Saved,
        Sent
    }
}
