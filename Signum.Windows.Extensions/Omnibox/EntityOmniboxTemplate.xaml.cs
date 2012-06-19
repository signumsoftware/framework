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
    public partial class EntityOmniboxTemplate : UserControl
    {
        public EntityOmniboxTemplate()
        {
            InitializeComponent();

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(EntityOmniboxTemplate_DataContextChanged);
        }

        void EntityOmniboxTemplate_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var result = e.NewValue as EntityOmniboxResult;

            var lines = textBlock.Inlines;

            lines.Clear();

            if (result == null)
                return;


            lines.Add("({0:0.0})".Formato(result.Distance));

            lines.AddRange(OmniboxClient.PackInlines(result.TypeMatch));

            lines.Add(" ");

            if (result.Id == null && result.ToStr == null)
            {
                lines.Add("...");
            }
            else
            {
                if (result.Id != null)
                {
                    lines.Add(result.Id.ToString());
                    lines.Add(": ");
                    if (result.Lite == null)
                    {
                        lines.Add(new Run(Signum.Entities.Extensions.Properties.Resources.NotFound) { Foreground = Brushes.Gray });
                    }
                    else
                    {
                        lines.Add(result.Lite.TryToString());
                    }
                }
                else
                {
                    if (result.Lite == null)
                    {
                        lines.Add("\"");
                        lines.Add(result.ToStr);
                        lines.Add("\": ");
                        lines.Add(new Run(Signum.Entities.Extensions.Properties.Resources.NotFound) { Foreground = Brushes.Gray });
                    }
                    else
                    {
                        lines.Add(result.Lite.Id.ToString());
                        lines.Add(": ");
                        lines.AddRange(OmniboxClient.PackInlines(result.ToStrMatch));
                    }
                }
            }
        }
    }
}
