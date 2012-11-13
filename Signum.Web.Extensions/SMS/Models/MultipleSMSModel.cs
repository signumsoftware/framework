using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities;
using Signum.Entities.SMS;

namespace Signum.Web.Extensions.SMS.Models
{
    public class MultipleSMSModel : ModelEntity
    {
        string message;
        [StringLengthValidator(AllowNulls = false, Max = SMSCharacters.SMSMaxTextLength)]
        public string Message
        {
            get { return message; }
            set { Set(ref message, value, () => Message); }
        }

        string from = SMSMessageDN.DefaultFrom;
        [StringLengthValidator(AllowNulls = false)]
        public string From
        {
            get { return from; }
            set { Set(ref from, value, () => From); }
        }

        string providerKeys;
        public string ProviderKeys
        {
            get { return providerKeys; }
            set { Set(ref providerKeys, value, () => ProviderKeys); }
        }

        string webTypeName;
        public string WebTypeName
        {
            get { return webTypeName; }
            set { Set(ref webTypeName, value, () => WebTypeName); }
        }
    }
}