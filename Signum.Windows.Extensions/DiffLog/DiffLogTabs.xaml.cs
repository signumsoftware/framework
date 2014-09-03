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

            if (mixin.InitialState != null)
            {
                var prev = minMax.Min;

                if (prev != null && prev.Mixin<DiffLogMixin>().FinalState != null)
                {
                    tabs.Items.Add(new LinkTabItem
                    {
                        Header = TextWithLinkIcon(DiffLogMessage.PreviousLog.NiceToString())
                        .Do(a => a.ToolTip = DiffLogMessage.NavigatesToThePreviousOperationLog.NiceToString()),
                        DataContext = prev,
                    });

                    tabs.Items.Add(new TabItem
                    {
                        Header = IconPair("fast-back.png", "back.png", prev.Mixin<DiffLogMixin>().FinalState == mixin.InitialState)
                        .Do(a => a.ToolTip = DiffLogMessage.DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState.NiceToString()),
                        Content = Diff(prev.Mixin<DiffLogMixin>().FinalState, mixin.InitialState),
                    });
                }

                tabs.Items.Add(selected = new TabItem
                {
                    Header = new TextBlock { Text = ReflectionTools.GetPropertyInfo(() => mixin.InitialState).NiceName() }
                        .Do(a => a.ToolTip = DiffLogMessage.StateWhenTheOperationStarted.NiceToString()),
                    Content = new TextBlock(new Run(mixin.InitialState)) { FontFamily = font },
                });
            }

            if (mixin.InitialState != null && mixin.FinalState != null)
            {
                tabs.Items.Add(bestSelected = new TabItem
                {
                    Header = IconPair("back.png", "fore.png", mixin.InitialState == mixin.FinalState)
                        .Do(a => a.ToolTip = DiffLogMessage.DifferenceBetweenInitialStateAndFinalState.NiceToString()),
                    Content = Diff(mixin.InitialState, mixin.FinalState),
                });
            }

            if (mixin.FinalState != null)
            {
                tabs.Items.Add(selected = new TabItem
                {
                    Header = new TextBlock { Text = ReflectionTools.GetPropertyInfo(() => mixin.FinalState).NiceName() }                    
                        .Do(a => a.ToolTip = DiffLogMessage.StateWhenTheOperationFinished.NiceToString()),
                    Content = new TextBlock(new Run(mixin.FinalState)) { FontFamily = font },
                });

                var next = minMax.Max;
                if (next != null && next.Mixin<DiffLogMixin>().InitialState != null)
                {
                    tabs.Items.Add(new TabItem
                    {
                        Header = IconPair("fore.png", "fast-fore.png", mixin.FinalState == next.Mixin<DiffLogMixin>().InitialState)                    
                        .Do(a => a.ToolTip = DiffLogMessage.DifferenceBetweenFinalStateAndTheInitialStateOfNextLog.NiceToString()),
                        Content = Diff(mixin.FinalState, next.Mixin<DiffLogMixin>().InitialState),
                    });

                    tabs.Items.Add(new LinkTabItem
                    {
                        Header = TextWithLinkIcon(DiffLogMessage.NextLog.NiceToString())
                        .Do(a => a.ToolTip = DiffLogMessage.NavigatesToTheNextOperationLog.NiceToString()),
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
                            Header = IconPair("fore.png", "fast-fore.png", mixin.FinalState == dump)
                            .Do(a => a.ToolTip = DiffLogMessage.DifferenceBetweenFinalStateAndTheInitialStateOfNextLog.NiceToString()),
                            Content = Diff(mixin.FinalState, dump),
                        });

                        tabs.Items.Add(new LinkTabItem
                        {
                            Header = TextWithLinkIcon(DiffLogMessage.CurrentEntity.NiceToString())
                            .Do(a => a.ToolTip = DiffLogMessage.NavigatesToTheCurrentEntity.NiceToString()),
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
                    },
                    new Border
                    { 
                        Child = new Image { Source =   ExtensionsImageLoader.GetImageSortName(second), Margin = margin }, 
                        Background = equal ? lightGreen : green, 
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
