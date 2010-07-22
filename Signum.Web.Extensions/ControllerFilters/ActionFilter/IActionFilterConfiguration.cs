using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Signum.Web
{
    public interface IActionFilterConfig
    {
        Dictionary<string, IList<FilterAttribute>> ActionFilterAddedToActions { get; }

        IEnumerable<FilterAttribute> ActionFilterAddedToController { get; }

    }
}
