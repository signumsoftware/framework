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

namespace Signum.Windows.Operations
{
    public class ConstructFromMenuItem : SearchControlMenuItem
    {
        internal List<OperationInfo> OperationInfos; 

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Header = "Construir";
            Icon = new Image { Width = 16, Height = 16, Source = new BitmapImage(PackUriHelper.Reference("Images/factory.png", typeof(ConstructFromMenuItem))) };
        }

        protected override void Initialize()
        {
            Items.Clear();

            this.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(MenuItem_Clicked));

            foreach (OperationInfo oi in OperationInfos)
            {
                MenuItem mi = new MenuItem()
                {
                    Header = OperationClient.GetText(oi.Key),
                    Icon = OperationClient.GetImage(oi.Key),
                    Tag = oi,
                };
                Items.Add(mi);
            }
        }

        private void MenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (e.OriginalSource is MenuItem) //Not to capture the mouseDown of the scrollbar buttons
            {
                MenuItem b = (MenuItem)e.OriginalSource;
                OperationInfo operationInfo = (OperationInfo)b.Tag;
                Type entityType = SearchControl.EntityType;
                object queryName = SearchControl.QueryName;

                var lites = SearchControl.SelectedItems.TryCC(a => a.Cast<Lite>().ToList());

                if (lites == null || lites.Count == 0)
                    throw new ApplicationException(Signum.Windows.Extensions.Properties.Resources.SelectSomeRowsFirst);

                ConstructorFromManySettings settings = (ConstructorFromManySettings)OperationClient.Manager.Settings.TryGetC(operationInfo.Key);

                IIdentifiable entity;
                if (settings != null && settings.Constructor != null)
                    entity = settings.Constructor(new ConstructorFromManyEventArgs
                    {
                        Entities = lites,
                        Window = b.FindCurrentWindow(),
                        OperationInfo = operationInfo,
                        QueryName = queryName
                    });
                else entity = Server.Return((IOperationServer s)=>s.ConstructFromMany(lites, entityType, operationInfo.Key)); 

                if (operationInfo.Returns && Navigator.IsViewable(entity.GetType(), false))
                    Navigator.Navigate(entity); 
            }
        }
    }
}
