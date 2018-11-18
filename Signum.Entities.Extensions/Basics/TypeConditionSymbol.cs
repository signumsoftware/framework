using System;

namespace Signum.Entities.Basics
{
    [Serializable]
    public class TypeConditionSymbol : Symbol
    {
        private TypeConditionSymbol() { }

        public TypeConditionSymbol(Type declaringType, string fieldName) :
            base(declaringType, fieldName)
        {
        }
    }
}
