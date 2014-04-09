using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master)]
    public class PermissionSymbol : Symbol
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public PermissionSymbol([CallerMemberName]string memberName = null) : 
            base(new StackFrame(1, false), memberName)
        {
        }
    }

    public enum BasicPermission
    {
        AdminRules,
    }
}
