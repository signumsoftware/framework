using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Entities.Processes;
using Signum.Utilities;
using Signum.Entities.Authorization;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Signum.Entities.Scheduler
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master)]
    public class SimpleTaskSymbol : Symbol, ITaskEntity
    {
        private SimpleTaskSymbol() { } 

        [MethodImpl(MethodImplOptions.NoInlining)]
        public SimpleTaskSymbol([CallerMemberName]string memberName = null) :
            base(new StackFrame(1, false), memberName)
        {
        }
    }
}
