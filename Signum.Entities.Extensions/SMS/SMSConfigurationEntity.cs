using System;
using Signum.Entities.Basics;

namespace Signum.Entities.SMS
{
    [Serializable]
    public class SMSConfigurationEmbedded : EmbeddedEntity
    {
        [NotNullValidator]
        public CultureInfoEntity DefaultCulture { get; set; }
    }
}
