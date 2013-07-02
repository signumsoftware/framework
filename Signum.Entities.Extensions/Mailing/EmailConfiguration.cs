using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities.Translation;

namespace Signum.Entities.Mailing
{
    [Serializable]
    public class EmailConfigurationDN : EmbeddedEntity
    {
        CultureInfoDN defaultCulture;
        [NotNullValidator]
        public CultureInfoDN DefaultCulture
        {
            get { return defaultCulture; }
            set { Set(ref defaultCulture, value, () => DefaultCulture); }
        }

        string urlLeft;
        public string UrlLeft
        {
            get { return urlLeft; }
            set { Set(ref urlLeft, value, () => UrlLeft); }
        }

        bool doNotSendEmails;
        public bool DoNotSendEmails
        {
            get { return doNotSendEmails; }
            set { Set(ref doNotSendEmails, value, () => DoNotSendEmails); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string overrideEmailAddress;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100), EMailValidator]
        public string OverrideEmailAddress
        {
            get { return overrideEmailAddress; }
            set { Set(ref overrideEmailAddress, value, () => OverrideEmailAddress); }
        }
    }
}
