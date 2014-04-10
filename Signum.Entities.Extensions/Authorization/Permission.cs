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
        private PermissionSymbol() { } 

        [MethodImpl(MethodImplOptions.NoInlining)]
        public PermissionSymbol([CallerMemberName]string memberName = null) : 
            base(new StackFrame(1, false), memberName)
        {
        }
    }

    public static class BasicPermission
    {
        public static readonly PermissionSymbol AdminRules = new PermissionSymbol();
    }
}
