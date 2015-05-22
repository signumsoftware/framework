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

        double? creationDateHoursLimitToSendEmails;
        public double? CreationDateHoursLimitToSendEmails
        {
            get { return creationDateHoursLimitToSendEmails; }
            set { Set(ref creationDateHoursLimitToSendEmails, value); }
        }

        int chunkSizeToProcessEmails = 100;
        public int ChunkSizeToProcessEmails
        {
            get { return chunkSizeToProcessEmails; }
            set { Set(ref chunkSizeToProcessEmails, value); }
        }

        int maxEmailSendRetries = 3;
        public int MaxEmailSendRetries
        {
            get { return maxEmailSendRetries; }
            set { Set(ref maxEmailSendRetries, value); }
        }

        int asyncSenderPeriodMilliseconds = (60 * 1000) * 60 * 5; //5 minutes
        public int AsyncSenderPeriodMilliseconds
        {
            get { return asyncSenderPeriodMilliseconds; }
            set { Set(ref asyncSenderPeriodMilliseconds, value); }
        }
    }
}
