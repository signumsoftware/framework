using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Basics;

[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class PermissionSymbol : Symbol
{
    private PermissionSymbol() { }

    public PermissionSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}
