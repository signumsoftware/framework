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

namespace Signum.Windows.Operations
{
    public static class ConstructFromManyMenuItemConsturctor
    {
        public static MenuItem Construct(SearchControl sc, OperationInfo oi, ContextualOperationSettings settings)
        {
            MenuItem miResult = new MenuItem
            {
                Header = OperationClient.GetText(oi.Key),
                Icon = OperationClient.GetImage(oi.Key),
            };

            miResult.Click += (object sender, RoutedEventArgs e) =>
            {
                Type entityType = sc.EntityType;
                object queryName = sc.QueryName;

                var lites = sc.SelectedItems;

                if (settings != null && settings.Click != null)
                    settings.Click(new ContextualOperationEventArgs
                    {
                        Entities = lites,
                        SearchControl = sc,
                        OperationInfo = oi,
                    });
                else
                {
                    IIdentifiable entity = Server.Return((IOperationServer s) => s.ConstructFromMany(lites.ToList(), entityType, oi.Key));

                    if (oi.Returns && Navigator.IsNavigable(entity.GetType(), isSearchEntity: true))
                        Navigator.Navigate(entity);
                }
            };

            return miResult;
        }
    }
}
