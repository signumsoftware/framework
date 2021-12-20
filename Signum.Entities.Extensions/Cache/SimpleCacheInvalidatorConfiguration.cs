using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Cache
{
    public class CacheConfigurationEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(Max = 100)]
        public string CommonSecret { get; set; }

        [PreserveOrder, NoRepeatValidator]
        public MList<ServereInstanceEmbedded> ServerInstances { get; set; } = new MList<ServereInstanceEmbedded>();

    }

    public class ServereInstanceEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(Max = 100), URLValidator(absolute: true)]
        public string Url { get; set; }
    }
}
