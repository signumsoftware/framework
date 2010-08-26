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

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            Grid g = (Grid)sender;
            Common.SetCurrentWindow(g, this.FindCurrentWindow());
            FilterOption f = (FilterOption)g.DataContext;
            Implementations implementations = f.Token.Implementations();
            Common.SetIsReadOnly(g, f.Frozen);
        
            Type type = f.Token.Type;
            if (typeof(Lite).IsAssignableFrom(type))
            {
                Type cleanType = Reflector.ExtractLite(type);

                if (Reflector.IsLowPopulation(cleanType) && !(implementations is ImplementedByAllAttribute))
                {
                    EntityCombo ec = new EntityCombo
                    {
                        Type = type,
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
                        Type = type,
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
            else if (typeof(EmbeddedEntity).IsAssignableFrom(type))
            {
                EntityLine el = new EntityLine
                {
                    Type = type,
                    Create = false,
                    AutoComplete = false,
                    Find = false,
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
            else
            {
                QueryToken token = f.Token;

                ValueLine vl = new ValueLine() 
                { 
                    Type = type, 
                    Format = token.Format, 
                    UnitText = token.Unit 
                };

                if (type.UnNullify().IsEnum)
                {
                    vl.ItemSource = EnumProxy.GetValues(type.UnNullify()).PreAndNull(type.IsNullable());
                }
               

                vl.SetBinding(ValueLine.ValueProperty, new Binding
                {
                    Path = new PropertyPath("RealValue"), //odd
                    NotifyOnValidationError = true,
                    ValidatesOnDataErrors = true,
                    ValidatesOnExceptions = true,
                    Converter = Reflector.IsNumber(type) ? Converters.Identity : null,
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

        public void AddFilter(QueryToken queryToken)
        {

            if (queryToken == null)
            {
                MessageBox.Show(Properties.Resources.NoFilterSelected);
                return;
            }

            FilterType ft = QueryUtils.GetFilterType(queryToken.Type);

            FilterOption f = new FilterOption
            {
                Token = queryToken,
                Value = null,
                Operation = QueryUtils.GetFilterOperations(ft).First()
            };

            Filters.Add(f);

            RefreshFirstColumn();
        }
    }
}