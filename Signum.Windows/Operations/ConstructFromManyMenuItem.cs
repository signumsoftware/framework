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
        public static MenuItem Construct(IContextualOperationContext coc)
        { 
            MenuItem miResult = new MenuItem
            {
                Header = OperationClient.GetText(coc.Type, coc.OperationInfo.OperationSymbol),
                Icon = OperationClient.GetImage(coc.Type, coc.OperationInfo.OperationSymbol).ToSmallImage(),
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
                coc.SearchControl.SetDirtySelectedItems();

                if (coc.OperationSettings != null && coc.OperationSettings.HasClick)
                    coc.OperationSettings.OnClick(coc);
                else
                {
                    if (coc.ConfirmMessage())
                    {
                        IdentifiableEntity result = (IdentifiableEntity)new ConstructorContext(coc.SearchControl, coc.OperationInfo).SurroundConstructUntyped(coc.OperationInfo.ReturnType, ctx =>
                            Server.Return((IOperationServer s) => s.ConstructFromMany(coc.SearchControl.SelectedItems.ToList(), coc.Type, coc.OperationInfo.OperationSymbol, ctx.Args)));

                        if (result != null)
                            Navigator.Navigate(result);
                        else
                            MessageBox.Show(Window.GetWindow(coc.SearchControl), OperationMessage.TheOperation0DidNotReturnAnEntity.NiceToString().Formato(coc.OperationInfo.OperationSymbol.NiceToString()));
                    }
                }
            };

            return miResult;
        }
    }
}
