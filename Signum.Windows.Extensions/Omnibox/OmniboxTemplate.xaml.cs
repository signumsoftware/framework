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
using Signum.Entities.DynamicQuery;
using Signum.Entities.Basics;

namespace Signum.Windows.Omnibox
{
    /// <summary>
    /// Interaction logic for EntityOmniboxTemplate.xaml
    /// </summary>
    public partial class OmniboxTemplate : UserControl
    {
        public Action<OmniboxTemplate, InlineCollection> RenderLines;

        public OmniboxTemplate()
        {
            InitializeComponent();

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(EntityOmniboxTemplate_DataContextChanged);
        }

        void EntityOmniboxTemplate_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var result = e.NewValue as OmniboxResult;

            var lines = textBlock.Inlines;

            lines.Clear();

            if (result == null)
                return;

            OmniboxClient.RenderLines.Invoke(result, lines);
        }
    }
}
