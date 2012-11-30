using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Extensions;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using Signum.Entities.Reports;
using Prop = Signum.Windows.Extensions.Properties;
using Signum.Services;
using Signum.Entities.Operations;
using Signum.Utilities;
using Signum.Windows.Extensions.Properties;
using Signum.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Signum.Windows.Operations;
using Signum.Entities.Processes;

namespace Signum.Windows.Processes
{

    public static class PackageOperationMenuItemConsturctor
    {
        public static MenuItem Construct(SearchControl sc, Enum operationKey, string canExecute, Dictionary<Type, OperationInfo> operationInfos, EntityOperationSettingsBase settings)
        {
            MenuItem miResult = new MenuItem
            {
                Header = settings.TryCC(s => s.ContextualFromMany.TryCC(f => f.Text) ?? s.Text) ?? operationKey.NiceToString(),
                Icon = settings.TryCC(s => s.ContextualFromMany.TryCC(f => f.Icon) ?? s.Icon),
                ToolTip = canExecute,
                IsEnabled = string.IsNullOrEmpty(canExecute)
            };

            miResult.Click += (object sender, RoutedEventArgs e) =>
            {
                Type entityType = sc.EntityType;
                object queryName = sc.QueryName;

                var lites = sc.SelectedItems;

                if (settings != null && settings.ContextualFromMany != null && settings.ContextualFromMany.Click != null)
                    settings.ContextualFromMany.Click(new ContextualOperationEventArgs
                     {
                         Entities = lites,
                         SearchControl = sc,
                         OperationInfo = operationInfos.Values.First(),
                     });
                else
                {
                    IIdentifiable entity = Server.Return((IOperationServer s) => s.CreatePackageOperation(lites.ToList(), operationKey));

                    Navigator.Navigate(entity);
                }
            };


            return miResult;
        }
    }
}
