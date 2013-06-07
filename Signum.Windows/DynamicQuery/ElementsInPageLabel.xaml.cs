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
                case PaginationMode.AllElements:
                    tb.Inlines.Add(new Run(rt.TotalElements.Value.ToString()) { FontWeight = FontWeights.Bold });
                    tb.Inlines.Add(new Run(" "));
                    tb.Inlines.Add(new Run(SearchMessage.Results.NiceToString()));
                    break;
                case PaginationMode.Top:
                    var top = (Pagination.Top)rt.Pagination;
                    var run = new Run(rt.TotalElements.Value.ToString());

                    if(rt.Rows.Length == top.TopElements)
                        run.Foreground = Brushes.Red;

                    tb.Inlines.Add(run);
                    tb.Inlines.Add(new Run(" "));
                    tb.Inlines.Add(new Run(SearchMessage.Results.NiceToString()));
                    break;
                case PaginationMode.Paginate:
                    tb.Inlines.Add(new Run(rt.StartElementIndex.Value.ToString()) { FontWeight = FontWeights.Bold });
                    tb.Inlines.Add(new Run(" - "));
                    tb.Inlines.Add(new Run(rt.EndElementIndex.Value.ToString()) { FontWeight = FontWeights.Bold });
                    tb.Inlines.Add(new Run(" "));
                    tb.Inlines.Add(new Run(QueryTokenMessage.Of.NiceToString()));
                    tb.Inlines.Add(new Run(" "));
                    tb.Inlines.Add(new Run(rt.TotalElements.Value.ToString()) { FontWeight = FontWeights.Bold });
                    break;
                default:
                    break;
            }
        }
    }
}
