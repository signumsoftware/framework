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

            BindParameter(param1, "Parameter1");
            BindParameter(param2, "Parameter2");
            BindParameter(param3, "Parameter3");


            this.Loaded += new RoutedEventHandler(OnLoad);
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(ChartToken_DataContextChanged);
        }

        private void BindParameter(ValueLine vl, string property)
        {
            vl.Bind(ValueLine.VisibilityProperty, "ScriptColumn." + property, Converters.NullToVisibility);
            vl.Bind(ValueLine.ItemSourceProperty, new MultiBinding
            {
                Bindings = 
                {
                    new Binding("ScriptColumn." + property),
                    new Binding("Token"),
                },
                Converter = EnumValues
            });
            vl.Bind(ValueLine.ValueLineTypeProperty, "ScriptColumn." + property + ".Type", ParameterType);
            vl.Bind(ValueLine.LabelTextProperty, "ScriptColumn." + property + ".Name");
        }

        public static IMultiValueConverter EnumValues = ConverterFactory.New((ChartScriptParameterDN csp, QueryTokenDN token) =>
        {
            if (csp == null || csp.Type != ChartParameterType.Enum)
                return null;

            var t = token.Try(tk => tk.Token);

            return csp.GetEnumValues()
                .Where(a => a.CompatibleWith(t))
                .Select(a => a.Name)
                .ToList();
        });

        public static IValueConverter ParameterType = ConverterFactory.New((ChartParameterType type) =>
        {
            if (type == ChartParameterType.Enum)
                return ValueLineType.Enum;

            if (type == ChartParameterType.Number)
                return ValueLineType.Number;

            if (type == ChartParameterType.String)
                return ValueLineType.String;

            return ValueLineType.String;
        });

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

            return QueryUtils.SubTokens(token, desc, SubTokensOptions.CanElement |  (ct.IsGroupKey == false ? SubTokensOptions.CanAggregate : 0));
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
