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
using System.Windows.Documents;
using Signum.Utilities;
using Signum.Entities.Chart;
using Signum.Entities.Authorization;
using Signum.Windows.Authorization;
using System.Windows.Data;
using Signum.Entities.UserQueries;
using Signum.Windows.UserAssets;
using Signum.Entities.UserAssets;

namespace Signum.Windows.Chart
{
    public class UserChartMenuItem : MenuItem
    {
        public static readonly DependencyProperty CurrentUserChartProperty =
            DependencyProperty.Register("CurrentUserChart", typeof(UserChartEntity), typeof(UserChartMenuItem), new UIPropertyMetadata((d, e) => ((UserChartMenuItem)d).UpdateCurrent((UserChartEntity)e.NewValue)));
        public UserChartEntity CurrentUserChart
        {
            get { return (UserChartEntity)GetValue(CurrentUserChartProperty); }
            set { SetValue(CurrentUserChartProperty, value); }
        }

        public ChartRequestWindow ChartWindow { get; set; }

        public UserChartMenuItem()
        {
            if (!Navigator.IsViewable(typeof(UserChartEntity)))
                Visibility = System.Windows.Visibility.Hidden;

            this.Loaded += new RoutedEventHandler(UserChartMenuItem_Loaded);
        }

        void UserChartMenuItem_Loaded(object sender, RoutedEventArgs e)
        {
            var currentEntity = UserAssetsClient.GetCurrentEntity(this);

            if (currentEntity != null)
                this.Visibility = System.Windows.Visibility.Hidden;
            else
                Initialize();

            var autoSet = ChartClient.GetUserChart(ChartWindow);

            if (autoSet != null)
                using (currentEntity == null ? null : CurrentEntityConverter.SetCurrentEntity(currentEntity))
                    SetCurrent(autoSet);
        }

        ChartRequest ChartRequest
        {
            get { return (ChartRequest)ChartWindow.DataContext; }
        }

        QueryDescription Description
        {
            get { return DynamicQueryServer.GetQueryDescription(ChartRequest.QueryName); }
        }

        private void UpdateCurrent(UserChartEntity current)
        {
            Header = new TextBlock
            {
                Inlines = 
                { 
                    new Run(
                    current == null ? typeof(UserChartEntity).NicePluralName() : current.DisplayName), 
                    UserCharts == null || UserCharts.Count==0 ? (Inline)new Run():  new Bold(new Run(" (" + UserCharts.Count + ")")) 
                }
            };

            foreach (var item in this.Items.OfType<MenuItem>().Where(mi => mi.IsCheckable))
            {
                item.IsChecked = ((Lite<UserChartEntity>)item.Tag).Is(current);
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Icon = ExtensionsImageLoader.GetImageSortName("favorite.png").ToSmallImage();
        }

        List<Lite<UserChartEntity>> UserCharts; 

        public void Initialize()
        {
            Items.Clear();

            this.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(MenuItem_Clicked));

            UserCharts = Server.Return((IChartServer s) => s.GetUserCharts(ChartRequest.QueryName));
            
            if (UserCharts.Count > 0)
            {
                foreach (Lite<UserChartEntity> uc in UserCharts.OrderBy(a => a.ToString()))
                {
                    MenuItem mi = new MenuItem()
                    {
                        IsCheckable = true,
                        Header = uc.ToString(),
                        Tag = uc,
                    };
                    Items.Add(mi);
                }
            }

            UpdateCurrent(CurrentUserChart);

            if (Navigator.IsNavigable(typeof(UserChartEntity), true))
            {
                Items.Add(new Separator());

                if (Navigator.IsCreable(typeof(UserChartEntity), true))
                {
                    Items.Add(new MenuItem()
                    {
                        Header = EntityControlMessage.Create.NiceToString(),
                        Icon = ExtensionsImageLoader.GetImageSortName("add.png").ToSmallImage()
                    }.Handle(MenuItem.ClickEvent, New_Clicked));
                }

                Items.Add(new MenuItem()
                {
                    Header = UserQueryMessage.Edit.NiceToString(),
                    Icon = ExtensionsImageLoader.GetImageSortName("edit.png").ToSmallImage()
                }.Handle(MenuItem.ClickEvent, Edit_Clicked)
                .Bind(MenuItem.VisibilityProperty, this, "CurrentUserChart", notNullAndEditable));

                Items.Add(new MenuItem()
                {
                    Header = EntityControlMessage.Remove.NiceToString(),
                    Icon = ExtensionsImageLoader.GetImageSortName("remove.png").ToSmallImage()
                }.Handle(MenuItem.ClickEvent, Remove_Clicked)
                .Bind(MenuItem.VisibilityProperty, this, "CurrentUserChart", notNullAndEditable));
            }


        }

        static IValueConverter notNullAndEditable = ConverterFactory.New((UserChartEntity uq) => uq != null && !Navigator.IsReadOnly(uq) ? Visibility.Visible : Visibility.Collapsed);


        private void MenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (e.OriginalSource is MenuItem b)
            {
                Lite<UserChartEntity> userChart = (Lite<UserChartEntity>)b.Tag;

                var uc = Server.Return((IChartServer s) => s.RetrieveUserChart(userChart));

                SetCurrent(uc);
            }
        }

        private void SetCurrent(UserChartEntity uc)
        {
            CurrentUserChart = uc;

            this.ChartWindow.Request = CurrentUserChart.ToRequest();

            this.ChartWindow.GenerateChart();
        }

        private void New_Clicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            UserChartEntity userChart = ChartRequest.ToUserChart();

            userChart = Navigator.View(userChart, new ViewOptions
            {
                AllowErrors = AllowErrors.No,
                View = new UserChart { QueryDescription = Description }
            });

            Initialize();
            if (userChart != null)
                CurrentUserChart = userChart;
        }

        private void Edit_Clicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            var d = Dispatcher;
            var desc = Description;

            Navigator.Navigate(CurrentUserChart.ToLite().Retrieve(), new NavigateOptions()
            {
                View = () => new UserChart { QueryDescription = desc },
                Closed = (s, args) => d.Invoke(() => Initialize())
            });
        }

        private void Remove_Clicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (MessageBox.Show(Window.GetWindow(this), UserQueryMessage.AreYouSureToRemove0.NiceToString().FormatWith(CurrentUserChart), UserQueryMessage.RemoveUserQuery.NiceToString(),
                MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                CurrentUserChart.ToLite().DeleteLite(UserChartOperation.Delete);

                CurrentUserChart = null;

                Initialize();
            }
        }

    }
}
