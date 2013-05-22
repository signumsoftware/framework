using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities.Translation;

namespace Signum.Entities.Mailing
{
    public interface IEmailLogicConfiguration
    {
        CultureInfoDN DefaultCulture { get; set; }
        string UrlPrefix { get; set; }
        string DefaultFrom { get; set; }
        string DefaultDisplayFrom { get; set; }
        string DefaultBCC { get; set; }
    }

    [Serializable]
    public class EmailLogicConfiguration : IEmailLogicConfiguration
    {
        public CultureInfoDN DefaultCulture { get; set; }
        public string UrlPrefix { get; set; }
        public string DefaultFrom { get; set; }
        public string DefaultDisplayFrom { get; set; }
        public string DefaultBCC { get; set; }
    }
}
