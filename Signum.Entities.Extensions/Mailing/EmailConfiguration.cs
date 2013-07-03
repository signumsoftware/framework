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

        bool sendEmails;
        public bool SendEmails
        {
            get { return sendEmails; }
            set { Set(ref sendEmails, value, () => SendEmails); }
        }

        [SqlDbType(Size = 100)]
        string overrideEmailAddress;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100), EMailValidator]
        public string OverrideEmailAddress
        {
            get { return overrideEmailAddress; }
            set { Set(ref overrideEmailAddress, value, () => OverrideEmailAddress); }
        }
    }
}
