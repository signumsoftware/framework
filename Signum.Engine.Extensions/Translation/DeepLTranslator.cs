using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Signum.Engine.Translation
{
    public class DeepLTranslator : ITranslator
    {
        public string DeepLKey;

        public Func<string?>? Proxy { get; }

        public DeepLTranslator(string deepLKey)
        {
            this.DeepLKey = deepLKey;
        }


        static readonly XNamespace Ns = XNamespace.Get("http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2");
        static readonly XNamespace ArrayNs = XNamespace.Get("http://schemas.microsoft.com/2003/10/Serialization/Arrays");
        

        public async Task<List<string?>?> TranslateBatchAsync(List<string> list, string from, string to)
        {
            //todo
                return null;
            
        }

        public bool AutoSelect() => true;

        public List<string?> TranslateBatch(List<string> list, string from, string to)
        {
            throw new NotImplementedException();
        }
    }

   
}
