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
    public static class EntityOperationMenuItemConsturctor
    {
        public static MenuItem Construct(IContextualOperationContext coc)
        {
            MenuItem miResult = new MenuItem()
            {
                Header = coc.OperationSettings?.Text ?? coc.OperationInfo.OperationSymbol.NiceToString(),
                Icon = coc.OperationSettings?.Icon.ToSmallImage(),
                Tag = coc,
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
                    var lite = coc.SearchControl.SelectedItems.Single();

                    if (coc.ConfirmMessage())
                    {
                        switch (coc.OperationInfo.OperationType)
                        {
                            case OperationType.Execute:
                                Server.Return((IOperationServer os) => os.ExecuteOperationLite(lite, coc.OperationInfo.OperationSymbol));
                                break;
                            case OperationType.Delete:
                                Server.Execute((IOperationServer os) => os.DeleteLite(lite, coc.OperationInfo.OperationSymbol));
                                break;
                            case OperationType.ConstructorFrom:
                                {
                                    var result = (Entity)new ConstructorContext(coc.SearchControl, coc.OperationInfo).SurroundConstructUntyped(coc.OperationInfo.ReturnType, ctx =>
                                        Server.Return((IOperationServer os) => os.ConstructFromLite(lite, coc.OperationInfo.OperationSymbol)));

                                    if (result == null)
                                    {
                                        MessageBox.Show(Window.GetWindow(miResult), 
                                            OperationMessage.TheOperation0DidNotReturnAnEntity.NiceToString(coc.OperationInfo.OperationSymbol.NiceToString()));
                                    }
                                    else
                                    {
                                        if (Navigator.IsNavigable(result, true))
                                            Navigator.Navigate(result);
                                    };
                                    break;
                                }
                            case OperationType.Constructor:
                            case OperationType.ConstructorFromMany:
                                throw new InvalidOperationException("Unexpected operation type");
                        }
                    }
                }
            };

            return miResult;
        }
    }
}
