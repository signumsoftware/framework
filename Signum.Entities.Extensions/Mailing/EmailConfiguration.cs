using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities.Translation;

namespace Signum.Entities.Mailing
{
    [Serializable]
    public class EmailConfigurationEmbedded : EmbeddedEntity
    {
        [NotNullValidator]
        public CultureInfoEntity DefaultCulture { get; set; }

        public string UrlLeft { get; set; }

        public bool SendEmails { get; set; }

        public bool ReciveEmails { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100), EMailValidator]
        public string OverrideEmailAddress { get; set; }

        [Unit("hs")]
        public double? AvoidSendingEmailsOlderThan { get; set; }

        public int ChunkSizeSendingEmails { get; set; } = 100;

        public int MaxEmailSendRetries { get; set; } = 3;

        [Unit("sec")]
        public int AsyncSenderPeriod { get; set; } = 5 * 60; //5 minutes
    }
}
