using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Signum.Entities.Basics;

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
