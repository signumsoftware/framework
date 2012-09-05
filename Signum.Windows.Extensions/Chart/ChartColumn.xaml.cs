using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Chart;
using Signum.Utilities;

namespace Signum.Windows.Chart
{
    /// <summary>
    /// Interaction logic for ChartToken.xaml
    /// </summary>
    public partial class ChartColumn : UserControl, IPreLoad
    {
        public event EventHandler PreLoad;
        public static IValueConverter IsInterval = ConverterFactory.New((QueryToken t) => t is IntervalQueryToken ? Visibility.Visible : Visibility.Hidden);

        public static readonly DependencyProperty GroupResultsProperty =
            DependencyProperty.Register("GroupResults", typeof(bool), typeof(ChartColumn), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (e, o) =>
                {
                    ChartColumn ct = (ChartColumn)e;
                    if (ct.IsLoaded)
                        ct.UpdateGroup();
                }));

        public bool GroupResults
        {
            get { return (bool)GetValue(GroupResultsProperty); }
            set { SetValue(GroupResultsProperty, value); }
        }


        public ChartColumn()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(OnLoad);
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(ChartToken_DataContextChanged);
        }

        void ChartToken_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var oldColumn = e.OldValue as ChartColumnDN;

            if (oldColumn != null)
                oldColumn.Notified -= UpdateGroup;

            var newColumn = e.NewValue as ChartColumnDN;

            if (newColumn != null)
                newColumn.Notified += UpdateGroup;
        }

        private List<QueryToken> token_SubTokensEvent(QueryToken token)
        {
            var ct = DataContext as ChartColumnDN;
            if (ct == null)
                return new List<QueryToken>();

            var desc = this.VisualParents().OfType<ChartBuilder>().First().Description;

            return ct.SubTokensChart(token, desc.Columns);
        }

        private void UpdateGroup()
        {
            token.UpdateTokenList();
        }

        public void OnLoad(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnLoad;

            if (PreLoad != null)
                PreLoad(this, EventArgs.Empty);

            UpdateGroup(); 
        }
    }
}
