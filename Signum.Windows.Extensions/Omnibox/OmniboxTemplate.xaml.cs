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
using System.Windows.Automation;
using System.Text.RegularExpressions;

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

            if (result is HelpOmniboxResult)
            {
                var helpResult = result as HelpOmniboxResult;

                lines.Add(new Italic(GetTextSpan(helpResult)));

                if (helpResult.ReferencedType != null)
                {
                    lines.Add(" ");
                    lines.Add(OmniboxClient.Providers.GetOrThrow(helpResult.ReferencedType).GetIcon());
                }
                AutomationProperties.SetName(this, "Help");
            }
            else
            {
                var provider = OmniboxClient.Providers.GetOrThrow(result.GetType());

                provider.RenderLinesBase(result, lines);

                lines.Add(" ");

                lines.Add(provider.GetIcon());

                AutomationProperties.SetName(this, provider.GetNameBase(result));
            }
        }

        private static Span GetTextSpan(HelpOmniboxResult helpResult)
        {
            var span = new Span();

            for (int i = 0; i < helpResult.Text.Length; )
            {
                var start = helpResult.Text.IndexOf('(', i);
                if (start == -1)
                {
                    span.Inlines.Add(helpResult.Text.Substring(i));
                    break;
                }

                span.Inlines.Add(helpResult.Text.Substring(i, start - i));
                start++;

                var end = helpResult.Text.IndexOf(')', start);
                if (end == -1)
                {
                    span.Inlines.Add(helpResult.Text.Substring(start));
                    break;
                }

                span.Inlines.Add(new Bold(new Run(helpResult.Text.Substring(start, end - start ))));

                i = end + 1;
            }
            return span;
        }
    }
}
