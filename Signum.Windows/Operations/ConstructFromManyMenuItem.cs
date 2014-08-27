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
using Signum.Entities.Basics;
using System.Windows.Automation;

namespace Signum.Windows.Operations
{
    public static class ConstructFromManyMenuItemConsturctor
    {
        public static MenuItem Construct(ContextualOperationContext coc)
        { 
            MenuItem miResult = new MenuItem
            {
                Header = OperationClient.GetText(coc.OperationInfo.OperationSymbol),
                Icon = OperationClient.GetImage(coc.OperationInfo.OperationSymbol).ToSmallImage(),
            };

            if (coc.OperationSettings != null && coc.OperationSettings.Order != 0)
                Common.SetOrder(miResult, coc.OperationSettings.Order);

            if (coc.CanExecute != null)
            {
                miResult.ToolTip = coc.CanExecute;
                miResult.IsEnabled = false;
                ToolTipService.SetShowOnDisabled(miResult, true);
                AutomationProperties.SetHelpText(miResult, coc.CanExecute);
            }

            miResult.Click += (object sender, RoutedEventArgs e) =>
            {
                Type entityType = coc.SearchControl.EntityType;

                coc.SearchControl.SetDirtySelectedItems();

                if (coc.OperationSettings != null && coc.OperationSettings.Click != null)
                    coc.OperationSettings.Click(coc);
                else
                {
                    if (coc.ConfirmMessage())
                    {
                        IIdentifiable result = (IdentifiableEntity)coc.SearchControl.SurroundConstruct(coc.OperationInfo.ReturnType, null, ctx =>
                        {
                            return coc.NullEntityMessage(
                                Server.Return((IOperationServer s) => s.ConstructFromMany(coc.SearchControl.SelectedItems.ToList(), entityType, coc.OperationInfo.OperationSymbol)));
                        });

                        if (result != null)
                            Navigator.Navigate(result);
                    }
                }
            };

            return miResult;
        }
    }
}
