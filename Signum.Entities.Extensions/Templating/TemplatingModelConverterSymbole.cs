using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Templating
{
    [Serializable]
    public class ModelConverterSymbol : Symbol
    {
        private ModelConverterSymbol() { }

        public ModelConverterSymbol(Type declaringType, string fieldName) :
            base(declaringType, fieldName)
        {
        }
    }
}
