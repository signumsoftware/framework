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
using Signum.Entities.Omnibox;
using Signum.Utilities;

namespace Signum.Windows.Omnibox
{
    /// <summary>
    /// Interaction logic for EntityOmniboxTemplate.xaml
    /// </summary>
    public partial class DynamicQueryOmniboxTemplate : UserControl
    {
        public DynamicQueryOmniboxTemplate()
        {
            InitializeComponent();

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(EntityOmniboxTemplate_DataContextChanged);
        }

        void EntityOmniboxTemplate_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var result = e.NewValue as DynamicQueryOmniboxResult;

            var lines = textBlock.Inlines;

            lines.Clear();

            if (result == null)
                return;


            lines.Add("({0:0.0})".Formato(result.Distance));

            lines.AddRange(OmniboxClient.PackInlines(result.QueryNameMatch));

            foreach (var item in result.Filters)
            {
                lines.Add(" ");

                bool first = true;
                if (item.QueryTokenPacks != null)
                {
                    foreach (var tokenPack in item.QueryTokenPacks)
                    {
                        if (first)
                            first = false;
                        else
                            lines.Add(".");

                        lines.AddRange(OmniboxClient.PackInlines(tokenPack));
                    }
                }

                if (item.SuggestedQueryToken != null)
                {
                    if(!first)
                        lines.Add(".");

                    lines.Add(new Run(item.SuggestedQueryToken.ToString()) { Foreground = Brushes.Gray });
                }
                else
                {

                    if (item.CanFilter.HasText())
                    {
                        lines.Add(new Run(item.CanFilter) { Foreground = Brushes.Red });
                    }
                    else if (item.Operation != null)
                    {
                        lines.Add(DynamicQueryOmniboxProvider.ToStringOperation(item.Operation.Value));

                        if (item.Value == DynamicQueryOmniboxProvider.UnknownValue)
                            lines.Add(new Run(Signum.Windows.Extensions.Properties.Resources.Unknown) { Foreground = Brushes.Red });
                        else if (item.ValuePack != null)
                            lines.AddRange(OmniboxClient.PackInlines(item.ValuePack));
                        else
                            lines.Add(DynamicQueryOmniboxProvider.ToStringValue(item.Value));
                    }
                }
            }
        }
    }
}
