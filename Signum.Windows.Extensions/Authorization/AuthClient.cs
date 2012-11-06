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

namespace Signum.Windows.Authorization
{
    public static class AuthClient
    {
        public static event Action UpdateCacheEvent;

        public static void UpdateCache()
        {
            if (UpdateCacheEvent != null)
                UpdateCacheEvent();
        }

        public static void Start(bool types, bool property, bool queries, bool permissions, bool operations, bool facadeMethods, bool defaultPasswordExpiresLogic)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Server.Connecting += UpdateCache;

                if (types) TypeAuthClient.Start();
                if (property) PropertyAuthClient.Start();
                if (queries) QueryAuthClient.Start();
                if (permissions) PermissionAuthClient.Start();
                if (facadeMethods) FacadeMethodAuthClient.Start();
                if (operations) OperationAuthClient.Start();

                UpdateCache();

                Navigator.AddSetting(new EntitySettings<UserDN>(EntityType.Main) { View = e => new User() });
                Navigator.AddSetting(new EntitySettings<RoleDN>(EntityType.Shared) { View = e => new Role() });
                Navigator.AddSetting(new EntitySettings<PasswordExpiresIntervalDN>(EntityType.Part) { View = e => new PasswordExpiresInterval() });

                OperationClient.AddSetting(new EntityOperationSettings<UserDN>(UserOperation.SaveNew)
                {
                    IsVisible = e=>e.Entity.IsNew,
                });

                OperationClient.AddSetting(new EntityOperationSettings<UserDN>(UserOperation.Save)
                {
                    IsVisible = e => !e.Entity.IsNew,
                }); 
            }
        }
    }
}
