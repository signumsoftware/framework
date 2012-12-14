using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Basics;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityType(EntityType.SystemString)]
    public class PermissionDN : MultiEnumDN
    {

    }

    public enum BasicPermissions
    {
        AdminRules,
    }
}
