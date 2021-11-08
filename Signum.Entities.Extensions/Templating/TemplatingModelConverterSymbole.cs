
namespace Signum.Entities.Templating
{
    public class ModelConverterSymbol : Symbol
    {
        private ModelConverterSymbol() { }

        public ModelConverterSymbol(Type declaringType, string fieldName) :
            base(declaringType, fieldName)
        {
        }
    }
}
