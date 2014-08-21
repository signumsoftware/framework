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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public TypeConditionSymbol( [CallerMemberName]string memberName = null) : 
            base(new StackFrame(1, false), memberName)
        {
        }
    }
}
