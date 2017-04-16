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
using Signum.Entities.UserQueries;
using Signum.Entities.UserAssets;

namespace Signum.Windows.Chart
{
    /// <summary>
    /// Interaction logic for ChartToken.xaml
    /// </summary>
    public partial class ChartColumn : UserControl, IPreLoad
    {
        public event EventHandler PreLoad;
      
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

            if (e.OldValue is ChartColumnEmbedded oldColumn)
                oldColumn.Notified -= UpdateGroup;


            if (e.NewValue is ChartColumnEmbedded newColumn)
                newColumn.Notified += UpdateGroup;
        }

        private List<QueryToken> token_SubTokensEvent(QueryToken token)
        {
            var ct = DataContext as ChartColumnEmbedded;
            if (ct == null)
                return new List<QueryToken>();

            var desc = this.VisualParents().OfType<ChartBuilder>().First().Description;

            return QueryUtils.SubTokens(token, desc, SubTokensOptions.CanElement |  (ct.IsGroupKey == false ? SubTokensOptions.CanAggregate : 0));
        }

        private void UpdateGroup()
        {
            token.UpdateTokenList();
        }

        public void OnLoad(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnLoad;

            PreLoad?.Invoke(this, EventArgs.Empty);

            UpdateGroup(); 
        }

        
    }
}
