using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Services; 

namespace Signum.Windows.Authorization
{
    /// <summary>
    /// Interaction logic for Usuario.xaml
    /// </summary>
    public partial class Role : UserControl, IHaveQuickLinks
    {
        public Role()
        {
            InitializeComponent();
        }

        Lazy<RoleDN> Lazy
        {
            get { return ((RoleDN)DataContext).ToLazy(); }
        }


        public List<QuickLink> QuickLinks()
        {
            List<QuickLink> links = new List<QuickLink>();

            if (!Server.Implements<IPermissionAuthServer>() || Server.Service<IPermissionAuthServer>().IsAuthorizedFor(BasicPermissions.AdminRules))
            {
                if (Server.Implements<IQueryAuthServer>())
                    links.Add(new QuickLink("Query Rules") { Action = () => new QueryRules { Role = Lazy }.Show() });

                if (Server.Implements<IServiceOperationAuthServer>())
                    links.Add(new QuickLink("Service Operation Rules") { Action = () => new ServiceOperationRules { Role = Lazy }.Show() });

                if (Server.Implements<ITypeAuthServer>())
                    links.Add(new QuickLink("Type Rules") { Action = () => new TypeRules { Role = Lazy }.Show() });

                if (Server.Implements<IPermissionAuthServer>())
                    links.Add(new QuickLink("Persmission Rules") { Action = () => new PermissionRules { Role = Lazy }.Show() });

                if (Server.Implements<IActionAuthServer>())
                    links.Add(new QuickLink("Action Rules") { Action = () => new ActionRules { Role = Lazy }.Show() });
            }

            return links;
        }
    }
}
