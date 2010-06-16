using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.Reports;
using Signum.Entities.Reports;
using Signum.Services;
using System.Reflection;

namespace Signum.Windows.Reports
{
    public class UserQueryClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryClient.Start();
                Navigator.Manager.Settings.Add(typeof(UserQueryDN), new EntitySettings(EntityType.Default));
                SearchControl.GetCustomMenuItems += (qn, type) => new UserQueryMenuItem();
                LiteFilterValueConverter.TryParseLite = Server.TryParseLite;
            }
        }
    }
}
