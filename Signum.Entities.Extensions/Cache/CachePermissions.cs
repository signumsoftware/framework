using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Cache
{
    public enum CachePermission
    {
        ViewCache,
        InvalidateCache
    }

    public interface IAfterClone : IIdentifiable
    {
        void AfterClone(IdentifiableEntity original);
    }
}
