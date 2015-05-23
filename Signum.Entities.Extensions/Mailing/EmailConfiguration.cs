using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities.Translation;

namespace Signum.Entities.Mailing
{
    [Serializable]
    public class EmailConfigurationEntity : EmbeddedEntity
    {
        CultureInfoEntity defaultCulture;
        [NotNullValidator]
        public CultureInfoEntity DefaultCulture
        {
            get { return defaultCulture; }
            set { Set(ref defaultCulture, value); }
        }

        string urlLeft;
        public string UrlLeft
        {
            get { return urlLeft; }
            set { Set(ref urlLeft, value); }
        }

        bool sendEmails;
        public bool SendEmails
        {
            get { return sendEmails; }
            set { Set(ref sendEmails, value); }
        }

        bool reciveEmails;
        public bool ReciveEmails
        {
            get { return reciveEmails; }
            set { Set(ref reciveEmails, value); }
        }


        [SqlDbType(Size = 100)]
        string overrideEmailAddress;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100), EMailValidator]
        public string OverrideEmailAddress
        {
            get { return overrideEmailAddress; }
            set { Set(ref overrideEmailAddress, value); }
        }

        double? avoidSendingEmailsOlderThan;
        [Unit("hs")]
        public double? AvoidSendingEmailsOlderThan
        {
            get { return avoidSendingEmailsOlderThan; }
            set { Set(ref avoidSendingEmailsOlderThan, value); }
        }

        int chunkSizeSendingEmails = 100;
        public int ChunkSizeSendingEmails
        {
            get { return chunkSizeSendingEmails; }
            set { Set(ref chunkSizeSendingEmails, value); }
        }

        int maxEmailSendRetries = 3;
        public int MaxEmailSendRetries
        {
            get { return maxEmailSendRetries; }
            set { Set(ref maxEmailSendRetries, value); }
        }

        int asyncSenderPeriod = 5 * 60; //5 minutes
        [Unit("sec")]
        public int AsyncSenderPeriod
        {
            get { return asyncSenderPeriod; }
            set { Set(ref asyncSenderPeriod, value); }
        }
    }
}
