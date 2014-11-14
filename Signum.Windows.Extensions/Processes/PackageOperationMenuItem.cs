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
        public static MenuItem Construct(IContextualOperationContext coc)
        {
            MenuItem miResult = new MenuItem
            {
                Header = coc.OperationSettings.Try(s => s.Text) ?? coc.OperationInfo.OperationSymbol.NiceToString(),
                Icon = coc.OperationSettings.Try(s => s.Icon.ToSmallImage()),
                Tag = coc,
            };

            if (coc.CanExecute != null)
            {
                miResult.ToolTip = coc.CanExecute;
                miResult.IsEnabled = false;
                ToolTipService.SetShowOnDisabled(miResult, true);
                AutomationProperties.SetHelpText(miResult, coc.CanExecute);
            }

            coc.SenderMenuItem = miResult;

            miResult.Click += (object sender, RoutedEventArgs e) =>
            {
                coc.SearchControl.SetDirtySelectedItems();

                if (coc.OperationSettings != null && coc.OperationSettings.HasClick)
                    coc.OperationSettings.OnClick(coc);
                else
                {
                    if (coc.ConfirmMessage())
                    {
                        IEntity entity = Server.Return((IProcessServer s) => s.CreatePackageOperation(coc.Entities.ToList(), coc.OperationInfo.OperationSymbol));

                        Navigator.Navigate(entity);
                    }
                }
            };

            return miResult;
        }
    }
}
