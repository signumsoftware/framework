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
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Entities;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for Pagination.xaml
    /// </summary>
    public partial class ElementsInPageLabel : UserControl
    {
        public ElementsInPageLabel()
        {
            InitializeComponent();
        }

        public void SetResults(ResultTable rt)
        {
            tb.Inlines.Clear();

            switch (rt.Pagination.GetMode())
            {
                case PaginationMode.All:
                    tb.Inlines.Add(SearchMessage._0Results_N.NiceToString().ForGenderAndNumber(number: rt.Rows.Length).FormatSpan(
                        new Run(rt.TotalElements.Value.ToString()) { FontWeight = FontWeights.Bold }));
                    break;
                case PaginationMode.Firsts:
                     var top = (Pagination.Firsts)rt.Pagination;
                     var run = new Run(rt.Rows.Length.ToString()) { FontWeight = FontWeights.Bold };
                    if(rt.Rows.Length == top.TopElements)
                        run.Foreground = Brushes.Red;

                    tb.Inlines.Add(SearchMessage.First0Results_N.NiceToString().ForGenderAndNumber(number: rt.Rows.Length).FormatSpan(run));
                    break;
                case PaginationMode.Paginate:
                    tb.Inlines.Add(SearchMessage._01of2Results_N.NiceToString().ForGenderAndNumber(number: rt.Rows.Length).FormatSpan(
                        new Run(rt.StartElementIndex.Value.ToString()) { FontWeight = FontWeights.Bold },
                        new Run(rt.EndElementIndex.Value.ToString()) { FontWeight = FontWeights.Bold },
                        new Run(rt.TotalElements.Value.ToString()) { FontWeight = FontWeights.Bold }));
                    break;
                default:
                    break;
            }
        }
    }
}
