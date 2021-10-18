using System;

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
