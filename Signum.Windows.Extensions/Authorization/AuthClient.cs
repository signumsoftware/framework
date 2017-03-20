using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Utilities;
using System.Windows;
using Signum.Services;
using System.Reflection;
using System.Collections;
using Signum.Windows;
using System.Windows.Controls;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Utilities.Reflection;
using Signum.Utilities.DataStructures;
using Signum.Windows.Operations;
using Signum.Windows.Omnibox;
using System.IO;
using Microsoft.Win32;

namespace Signum.Windows.Authorization
{
    public static class AuthClient
    {
        public static event Action UpdateCacheEvent;

        public static void UpdateCache()
        {
            UpdateCacheEvent?.Invoke();
        }

        public static void Start(bool types, bool property, bool queries, bool permissions, bool operations, bool defaultPasswordExpiresLogic)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Server.Connecting += UpdateCache;

                if (types) TypeAuthClient.Start();
                if (property) PropertyAuthClient.Start();
                if (queries) QueryAuthClient.Start();
                if (permissions) PermissionAuthClient.Start();
                if (operations) OperationAuthClient.Start();

                UpdateCache();

                Navigator.AddSetting(new EntitySettings<UserEntity> { View = e => new User(), Icon = ImageLoader.GetImageSortName("user.png") });
                Navigator.AddSetting(new EntitySettings<RoleEntity> { View = e => new Role(), Icon = ImageLoader.GetImageSortName("role.png") });

                if (defaultPasswordExpiresLogic)
                    Navigator.AddSetting(new EntitySettings<PasswordExpiresIntervalEntity> { View = e => new PasswordExpiresInterval() });

                OperationClient.AddSettings(new List<OperationSettings>()
                {
                    new EntityOperationSettings<UserEntity>(UserOperation.SetPassword){ IsVisible = e => false },
                    new EntityOperationSettings<UserEntity>(UserOperation.SaveNew){ IsVisible = e => e.Entity.IsNew },
                    new EntityOperationSettings<UserEntity>(UserOperation.Save) { IsVisible = e => !e.Entity.IsNew }
                });

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("UpdateAuthCache",
                    () => true,
                    win =>
                    {
                        UpdateCache();

                        MessageBox.Show(AuthMessage.AuthorizationCacheSuccessfullyUpdated.NiceToString());
                    }));

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("DownloadAuthRules",
                    () => BasicPermission.AdminRules.IsAuthorized(),
                    win =>
                    {
                        SaveFileDialog sfc = new SaveFileDialog()
                        {
                            FileName = "AuthRules.xml"
                        };
                        if (sfc.ShowDialog() == true)
                        {
                            var bytes = Server.Return((ILoginServer ls) => ls.DownloadAuthRules());

                            File.WriteAllBytes(sfc.FileName, bytes);
                        }
                    }));
            }
        }
    }
}
