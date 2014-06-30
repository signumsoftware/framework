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
                Header = coc.OperationSettings.Try(s => s.Text) ?? coc.OperationInfo.OperationSymbol.NiceToString(),
                Icon = coc.OperationSettings.Try(s => s.Icon.ToSmallImage()),
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
                coc.SearchControl.SetDirtySelectedItems();

                if (coc.OperationSettings != null && coc.OperationSettings.Click != null)
                    coc.OperationSettings.Click(new ContextualOperationContext
                     {
                         Entities = coc.Entities,
                         SearchControl = coc.SearchControl,
                         OperationInfo = coc.OperationInfo,
                     });
                else
                {
                    if (coc.ConfirmMessage())
                    {
                        IIdentifiable entity = Server.Return((IProcessServer s) => s.CreatePackageOperation(coc.Entities.ToList(), coc.OperationInfo.OperationSymbol));

                        Navigator.Navigate(entity);
                    }
                }
            };

            return miResult;
        }
    }
}
