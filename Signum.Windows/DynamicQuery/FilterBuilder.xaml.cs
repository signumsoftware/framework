using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;
using Signum.Entities;
using System.Windows.Input;
using System.Collections.Generic;
using System.Globalization;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;

namespace Signum.Windows
{
	public partial class FilterBuilder
	{
        public static readonly DependencyProperty FiltersProperty =
            DependencyProperty.Register("Filters", typeof(FreezableCollection<FilterOption>), typeof(FilterBuilder), new UIPropertyMetadata(null));
        public FreezableCollection<FilterOption> Filters
        {
            get { return (FreezableCollection<FilterOption>)GetValue(FiltersProperty); }
            set { SetValue(FiltersProperty, value); }
        }

        public DragController FilterDragController {get;set;}

        public FilterBuilder()
        {
            FilterDragController = new DragController(fe => {
                FilterOption fo = ((FilterOption)((QueryTokenRenderer)fe).DataContext);
                return new FilterOption { Token = fo.Token, Operation = fo.Operation, RealValue = fo.RealValue };               
            }, DragDropEffects.Copy);
            this.InitializeComponent();
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            FilterOption f = (FilterOption)cb.DataContext;
            FilterType ft = QueryUtils.GetFilterType(f.Token.Type);
            cb.ItemsSource = QueryUtils.GetFilterOperations(ft);
            cb.IsEnabled = !f.Frozen;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            Grid g = (Grid)sender;
            Common.SetCurrentWindow(g, this.FindCurrentWindow());
            FilterOption f = (FilterOption)g.DataContext;
            Implementations implementations = f.Token.Implementations();
            Common.SetIsReadOnly(g, f.Frozen);
        
            Type type = f.Token.Type;
            if (typeof(Lite).IsAssignableFrom(type) || typeof(IdentifiableEntity).IsAssignableFrom(type))
            {
                Type cleanType = typeof(Lite).IsAssignableFrom(type) ? Reflector.ExtractLite(type) : type;
                if (Reflector.IsLowPopulation(cleanType))
                {
                    EntityCombo ec = new EntityCombo
                    {
                        Type = Reflector.GenerateLite(cleanType),
                        Style = (Style)FindResource("toolTip"),
                        Implementations = implementations
                    };
                    ec.SetBinding(EntityCombo.EntityProperty, new Binding
                    {
                        Path = new PropertyPath(FilterOption.RealValueProperty),
                        NotifyOnValidationError = true,
                        ValidatesOnDataErrors = true,
                        ValidatesOnExceptions = true,
                    });
                    g.Children.Add(ec);
                }
                else
                {
                    EntityLine el = new EntityLine
                    {
                        Type = Reflector.GenerateLite(cleanType),
                        Create = false,
                        HideAutoCompleteOnLostFocus = false,
                        Implementations = implementations
                    };
                    el.SetBinding(EntityLine.EntityProperty, new Binding
                    {
                        Path = new PropertyPath(FilterOption.RealValueProperty),
                        NotifyOnValidationError = true,
                        ValidatesOnDataErrors = true,
                        ValidatesOnExceptions = true
                    });
                    g.Children.Add(el);
                }
            }
            else
            {
                QueryToken token = f.Token;

                ValueLine vl = new ValueLine() { Type = type, Format = token.Format, UnitText = token.Unit};
                vl.SetBinding(ValueLine.ValueProperty, new Binding
                {
                    Path = new PropertyPath("RealValue"),
                    NotifyOnValidationError = true,
                    ValidatesOnDataErrors = true,
                    ValidatesOnExceptions = true,
                    Converter = Reflector.IsNumber(type) ?  Converters.Identity: null,
                });
                g.Children.Add(vl);
            }
        }

        private void btRemove_Click(object sender, RoutedEventArgs e)
        {
            RemoveFilters();
        }

        private void RemoveFilters()
        {
            var toRemove = lvFilters.SelectedItems.Cast<FilterOption>().ToList();
            foreach (var f in toRemove)
            {
                Filters.Remove(f);
            }
        }

        private void lvFilters_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                RemoveFilters();
            }
        }

        private void lvFilters_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(FilterOption)) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void lvFilters_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FilterOption)))
            {
                FilterOption filter = (FilterOption)e.Data.GetData(typeof(FilterOption));

                Filters.Add(filter);

                RefreshFirstColumn();
            }
        }

        public void RefreshFirstColumn()
        {
            firstColumn.Width = 0;
            firstColumn.Width = double.NaN;
        }
    }
}