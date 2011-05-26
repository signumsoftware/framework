using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;

namespace Signum.Engine.Extensions.SMS
{
    public interface ISMSDestinationOwner: IIdentifiable
    {
        string DestinationNumber { get; }
    }
}
