using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Services;
using Signum.Entities.Authorization;
using Signum.Entities;

namespace Signum.Windows.Authorization
{
    public static class FacadeMethodAuthClient
    {
        public static bool Started { get; private set; }

        internal static void Start()
        {
            Started = true;

            Links.RegisterEntityLinks<RoleDN>((r, c) =>
            {
                bool authorized = BasicPermissions.AdminRules.TryIsAuthorized() ?? true;
                return new QuickLink[]
                {
                        new QuickLinkAction("Facade Method Rules", () => new FacadeMethodRules { Role = r.ToLite(), Owner = c.FindCurrentWindow() }.Show())
                        { 
                            IsVisible = authorized
                        },
                };
            });
        }

    }
}
