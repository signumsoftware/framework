using Signum.Entities.Basics;

namespace Signum.Entities.SMS;

public class SMSConfigurationEmbedded : EmbeddedEntity
{
    public CultureInfoEntity DefaultCulture { get; set; }
}
