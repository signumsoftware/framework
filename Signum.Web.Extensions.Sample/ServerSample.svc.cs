using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Engine.Reports;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Files;
using Signum.Entities.Reports;
using Signum.Services;
using Signum.Utilities;
using System.Threading;
using System.Globalization;
using Signum.Test.Extensions;
using Signum.Utilities.DataStructures;
using Signum.Entities.DynamicQuery;

namespace Signum.Web.Extensions.Sample
{
    public class ServerSample: ServerExtensions, IServerSample
    {

    }
}
