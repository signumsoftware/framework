using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using Signum.Web.Omnibox;
using Signum.Web.UserAssets;

namespace Signum.Web.UserAssets
{
    public class UserAssetsClient
    {
        public static string ViewPrefix = "~/UserAssets/Views/{0}.cshtml";

        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/UserAssets/Scripts/UserAssets"); 

        internal static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(UserAssetsClient));

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("ImportUserAssets", () => UserAssetPermission.UserAssetsToXML.IsAuthorized(), url =>
                    url.Action((UserAssetController a)=>a.Import())));
            }
        }

        internal static void RegisterExportAssertLink<T>() where T : Entity, IUserAssetEntity
        {
            LinksClient.RegisterEntityLinks<T>((lite, ctx) => new[]
            {
               new QuickLinkAction(UserAssetMessage.ExportToXml, RouteHelper.New().Action((UserAssetController a)=>a.Export(lite)))
               {
                   IsVisible = UserAssetPermission.UserAssetsToXML.IsAuthorized()
               }
            });
        }
    }
}