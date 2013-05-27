using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities.Translation;

namespace Signum.Entities.Mailing
{
    [Serializable]
    public class EmailLogicConfiguration : EmbeddedEntity
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
    }
}
