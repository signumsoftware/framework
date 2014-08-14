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
using Signum.Windows;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities.Reflection;
using Signum.Entities.DiffLog;
using Signum.Utilities;
using Signum.Services;

namespace Signum.Windows.DiffLog
{
    /// <summary>
    /// Interaction logic for DiffLogTabs.xaml
    /// </summary>
    public partial class DiffLogTabs : UserControl
    {
        public DiffLogTabs()
        {
            InitializeComponent();
            this.DataContextChanged += OperationLog_DataContextChanged;
            this.tabs.PreviewMouseDown += tabs_PreviewMouseDown;
        }

        void tabs_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var tab = ((DependencyObject)e.OriginalSource).LogicalParents().OfType<LinkTabItem>().FirstOrDefault();
            if (tab != null)
            {
                Navigator.Navigate((IdentifiableEntity)tab.DataContext);
                e.Handled = true;
            }
        
        }

        void OperationLog_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            tabs.Items.Clear();

            OperationLogDN log = (OperationLogDN)e.NewValue;

            if (log == null)
                return;

            var mixin = log.Mixin<DiffLogMixin>();

            var minMax = Server.Return((IDiffLogServer s) => s.OperationLogNextPrev(log));

            TabItem selected = null;
            TabItem bestSelected = null; 

            if (mixin.StartGraph != null)
            {
                var prev = minMax.Min;

                if (prev != null && prev.Mixin<DiffLogMixin>().EndGraph != null)
                {
                    tabs.Items.Add(new LinkTabItem
                    {
                        Header = TextWithLinkIcon(DiffLogMessage.PreviousLog.NiceToString()),
                        DataContext = prev,
                    });

                    tabs.Items.Add(new TabItem
                    {
                        Header = IconPair("fast-back.png", "back.png", prev.Mixin<DiffLogMixin>().EndGraph == mixin.StartGraph),
                        Content = Diff(prev.Mixin<DiffLogMixin>().EndGraph, mixin.StartGraph),
                    });
                }

                tabs.Items.Add(selected = new TabItem
                {
                    Header = ReflectionTools.GetPropertyInfo(() => mixin.StartGraph).NiceName(),
                    Content = new TextBlock(new Run(mixin.StartGraph)) { FontFamily = font },
                });
            }

            if (mixin.StartGraph != null && mixin.EndGraph != null)
            {
                tabs.Items.Add(bestSelected = new TabItem
                {
                    Header = IconPair("back.png", "fore.png", mixin.StartGraph == mixin.EndGraph),
                    Content = Diff(mixin.StartGraph, mixin.EndGraph),
                });
            }

            if (mixin.EndGraph != null)
            {
                tabs.Items.Add(selected = new TabItem
                {
                    Header = ReflectionTools.GetPropertyInfo(() => mixin.EndGraph).NiceName(),
                    Content = new TextBlock(new Run(mixin.EndGraph)) { FontFamily = font },
                });

                var next = minMax.Max;
                if (next != null && next.Mixin<DiffLogMixin>().StartGraph != null)
                {
                    tabs.Items.Add(new TabItem
                    {
                        Header = IconPair("fore.png", "fast-fore.png", mixin.EndGraph == next.Mixin<DiffLogMixin>().StartGraph),
                        Content = Diff(mixin.EndGraph, next.Mixin<DiffLogMixin>().StartGraph),
                    });

                    tabs.Items.Add(new LinkTabItem
                    {
                        Header = TextWithLinkIcon(DiffLogMessage.NextLog.NiceToString()),
                        DataContext = next,
                    });
                }
                else
                {
                    var entity = Server.Exists(log.Target) ? log.Target.RetrieveAndForget() : null;

                    if (entity != null)
                    {
                        var dump = entity.Dump();

                        tabs.Items.Add(new TabItem
                        {
                            Header = IconPair("fore.png", "fast-fore.png", mixin.EndGraph == dump),
                            Content = Diff(mixin.EndGraph, dump),
                        });

                        tabs.Items.Add(new LinkTabItem
                        {
                            Header = TextWithLinkIcon(DiffLogMessage.CurrentEntity.NiceToString()),
                            DataContext = entity,
                        });
                    }
                }
            }

            tabs.SelectedItem = bestSelected ?? selected;
        }

        static FontFamily font = new FontFamily("Menlo, Monaco, Consolas, 'Courier New', monospace");

        static Brush lightGreen = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CEF3CE")).Do(a => a.Freeze());
        static Brush green = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#72F272")).Do(a => a.Freeze());

        static Brush lightRed = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D1")).Do(a => a.Freeze());
        static Brush red = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8B8B")).Do(a => a.Freeze());

        private StackPanel TextWithLinkIcon(string text)
        {
            var margin = new Thickness(3, 0, 3, 0);

            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new TextBlock(new Run(text)), 
                    new Border { Child =  (Path)FindResource("Navigate"), Margin = margin }
                }
            };
        }

        private StackPanel IconPair(string first, string second, bool equal)
        {
            var margin = new Thickness(3, 0, 3, 0);

            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new Border
                    { 
                        Child = new Image { Source =   ExtensionsImageLoader.GetImageSortName(first), Margin = margin },
                        Background = equal ? lightRed : red, 
                        Margin = margin
                    },
                    new Border
                    { 
                        Child = new Image { Source =   ExtensionsImageLoader.GetImageSortName(second), Margin = margin }, 
                        Background = equal ? lightGreen : green, 
                        Margin = margin
                    },
                }
            };
        }


        public static TextBlock Diff(string oldStr, string newStr)
        {
            StringDistance sd = new StringDistance();

            var dif = sd.DiffText(oldStr, newStr);

            TextBlock tb = new TextBlock { FontFamily = font };
            foreach (var line in dif)
            {
                if (line.Action == StringDistance.DiffAction.Removed)
                {
                    tb.Inlines.Add(DiffLine(line.Value, lightRed));
                }
                if (line.Action == StringDistance.DiffAction.Added)
                {
                    tb.Inlines.Add(DiffLine(line.Value, lightGreen));
                }
                else if (line.Action == StringDistance.DiffAction.Equal)
                {
                    if (line.Value.Count == 1)
                    {
                        tb.Inlines.Add(DiffLine(line.Value, null));
                    }
                    else
                    {
                        tb.Inlines.Add(DiffLine(line.Value.Where(a => a.Action == StringDistance.DiffAction.Removed || a.Action == StringDistance.DiffAction.Equal), lightRed));
                        tb.Inlines.Add(DiffLine(line.Value.Where(a => a.Action == StringDistance.DiffAction.Added || a.Action == StringDistance.DiffAction.Equal), lightGreen));
                    }
                }
            }

            return tb;
        }

        private static Span DiffLine(IEnumerable<StringDistance.DiffPair<string>> list, Brush lineColor)
        {
            var span = new Span { Background = lineColor };

            foreach (var gr in list.GroupWhenChange(a => a.Action))
            {
                string text = gr.Select(a => a.Value).ToString("");

                if (gr.Key == StringDistance.DiffAction.Equal)
                    span.Inlines.Add(new Run(text));
                else
                {
                    var color =
                        gr.Key == StringDistance.DiffAction.Added ? green :
                        gr.Key == StringDistance.DiffAction.Removed ? red :
                        new InvalidOperationException().Throw<Brush>();

                    span.Inlines.Add(new Run(text) { Background = color });
                }
            }

            span.Inlines.Add(new LineBreak());

            return span;
        }
    }

    public class LinkTabItem : TabItem
    {
        
    }
}
