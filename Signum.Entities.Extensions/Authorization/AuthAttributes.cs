using System;
using System.Collections.Generic;
using System.Text;

namespace Signum.Entities.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class AllowedNoUserAttribute : Attribute
    {

    }
}
