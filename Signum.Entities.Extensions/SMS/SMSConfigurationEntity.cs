using System;
using Signum.Entities.Basics;

namespace Signum.Entities.SMS
{
    [Serializable]
    public class SMSConfigurationEmbedded : EmbeddedEntity
    {
        public CultureInfoEntity DefaultCulture { get; set; }
    }
}
