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

namespace Signum.Windows.Operations
{
    public static class EntityOperationMenuItemConsturctor
    {
        public static MenuItem Construct(SearchControl sc, OperationInfo oi, string canExecute,   EntityOperationSettingsBase settings)
        {
            MenuItem miResult = new MenuItem()
            {
                Header = settings.TryCC(s => s.Contextual.TryCC(f => f.Text) ?? s.Text) ?? oi.Key.NiceToString(),
                Icon = settings.TryCC(s => s.Contextual.TryCC(f => f.Icon) ?? s.Icon),
                ToolTip = canExecute,
                IsEnabled = string.IsNullOrEmpty(canExecute),
            }.Set(ToolTipService.ShowOnDisabledProperty, true);

            miResult.Click += (object sender, RoutedEventArgs e) =>
            {
                Type entityType = sc.EntityType;
                object queryName = sc.QueryName;

            
                if (settings != null && settings.Contextual != null && settings.Contextual.Click != null)
                    settings.Contextual.Click(new ContextualOperationEventArgs
                    {
                        Entities = sc.SelectedItems,
                        SearchControl = sc,
                        OperationInfo = oi,
                    });
                else
                {
                    var lite = sc.SelectedItems.Single();

                    switch (oi.OperationType)
                    {
                        case OperationType.Execute: 
                            Server.Return((IOperationServer os) => os.ExecuteOperationLite(lite, oi.Key)); 
                            break;
                        case OperationType.Delete: 
                            Server.Return((IOperationServer os) => os.Delete(lite, oi.Key));
                            break;
                        case OperationType.ConstructorFrom:
                            {
                                var result = Server.Return((IOperationServer os) => os.ConstructFromLite(lite, oi.Key));
                                if (Navigator.IsNavigable(result, true))
                                    Navigator.Navigate(result);
                                break;
                            }
                        case OperationType.Constructor:
                        case OperationType.ConstructorFromMany:
                            throw new InvalidOperationException("Unexpected operation type");
                    }
                }
            };

            return miResult;
        }
    }
}
