using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using Signum.Entities.Reports;
using Signum.Services;
using Signum.Utilities;
using Signum.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Signum.Windows.Operations;
using Signum.Entities.Processes;
using Signum.Entities.Basics;
using System.Windows.Automation;

namespace Signum.Windows.Processes
{
    public static class PackageOperationMenuItemConsturctor
    {
        public static MenuItem Construct(ContextualOperationContext coc)
        {
            MenuItem miResult = new MenuItem
            {
                Header = coc.OperationSettings.TryCC(s => s.Text) ?? coc.OperationInfo.Key.NiceToString(),
                Icon = coc.OperationSettings.TryCC(s => s.Icon),
            };

            if (coc.CanExecute != null)
            {
                miResult.ToolTip = coc.CanExecute;
                miResult.IsEnabled = false;
                ToolTipService.SetShowOnDisabled(miResult, true);
                AutomationProperties.SetHelpText(miResult, coc.CanExecute);
            }

            miResult.Click += (object sender, RoutedEventArgs e) =>
            {
                if (coc.OperationSettings != null && coc.OperationSettings.Click != null)
                    coc.OperationSettings.Click(new ContextualOperationContext
                     {
                         Entities = coc.Entities,
                         SearchControl = coc.SearchControl,
                         OperationInfo = coc.OperationInfo,
                     });
                else
                {
                    IIdentifiable entity = Server.Return((IProcessServer s) => s.CreatePackageOperation(coc.Entities.ToList(), coc.OperationInfo.Key));

                    Navigator.Navigate(entity);
                }
            };

            return miResult;
        }
    }
}
