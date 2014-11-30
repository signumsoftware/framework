using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Signum.Entities;
using Signum.Entities.Dashboard;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using Signum.Services;
using Signum.Utilities;
using Signum.Windows.Authorization;
using Signum.Windows.Omnibox;

namespace Signum.Windows.UserAssets
{
    public static class UserAssetsClient
    {
        public static readonly DependencyProperty CurrentEntityProperty =
            DependencyProperty.RegisterAttached("CurrentEntity", typeof(Entity), typeof(UserAssetsClient), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        public static Entity GetCurrentEntity(DependencyObject obj)
        {
            return (Entity)obj.GetValue(CurrentEntityProperty);
        }
        public static void SetCurrentEntity(DependencyObject obj, Entity value)
        {
            obj.SetValue(CurrentEntityProperty, value);
        }

        internal static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("ImportUserAssets", () => UserAssetPermission.UserAssetsToXML.IsAuthorized(), win =>
                {
                    OpenFileDialog ofd = new OpenFileDialog
                    {
                        DefaultExt = ".xml",
                    };

                    if (ofd.ShowDialog() != true)
                        return;

                    byte[] bytes = File.ReadAllBytes(ofd.FileName);

                    ImportUserAssetsConfirmation config = new ImportUserAssetsConfirmation
                    {
                        DataContext = Server.Return((IUserAssetsServer s) => s.PreviewAssetImport(bytes))
                    };

                    if (config.ShowDialog() == true)
                        Server.Execute((IUserAssetsServer s) => s.AssetImport(bytes, (UserAssetPreviewModel)config.DataContext));
                }));
            }
        }

        public static void RegisterExportAssertLink<T>() where T : Entity, IUserAssetEntity 
        {
            LinksClient.RegisterEntityLinks<T>((lite, control)=>new []
            {
               new QuickLinkAction(UserAssetMessage.ExportToXml, ()=>
               {
                   SaveFileDialog sfd = new SaveFileDialog
                   {
                       FileName = "{0}{1}.xml".FormatWith(lite.EntityType.Name, lite.Id),
                       DefaultExt = ".xml",
                       Filter = "UserAssets file (*.xml)|*.xml"
                   };

                   Window win = Window.GetWindow(control);

                   if (sfd.ShowDialog(win) == true)
                   {
                       var bytes = Server.Return((IUserAssetsServer s)=>s.ExportAsset(lite));

                       File.WriteAllBytes(sfd.FileName, bytes);  
                   }
               }){IsVisible = UserAssetPermission.UserAssetsToXML.IsAuthorized() }
            }); 
        }
    }
}
