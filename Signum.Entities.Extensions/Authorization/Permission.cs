using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Basics;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master)]
    public class PermissionDN : MultiEnumDN
    {

    }

    public enum BasicPermission
    {
        AdminRules,
    }
}
