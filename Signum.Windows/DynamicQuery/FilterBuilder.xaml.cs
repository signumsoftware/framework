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

        public DragController FilterDragController { get; set; }

        public FilterBuilder()
        {
            FilterDragController = new DragController(fe =>
            {
                FilterOption fo = ((FilterOption)((QueryTokenRenderer)fe).DataContext);
                return new FilterOption { Token = fo.Token, Operation = fo.Operation, RealValue = fo.RealValue };
            }, DragDropEffects.Copy);

            this.InitializeComponent();
        }

           
        private void filterRemove_Click(object sender, EventArgs e)
        {
            Filters.Remove((FilterOption)((FilterLine)sender).DataContext);
                }


        //private void lvFilters_DragOver(object sender, DragEventArgs e)
        //{
        //    e.Effects = e.Data.GetDataPresent(typeof(FilterOption)) ? DragDropEffects.Copy : DragDropEffects.None;
        //}

        //private void lvFilters_Drop(object sender, DragEventArgs e)
        //{
        //    if (e.Data.GetDataPresent(typeof(FilterOption)))
        //    {
        //        FilterOption filter = (FilterOption)e.Data.GetData(typeof(FilterOption));

        //        Filters.Add(filter);
        //    }
        //}


        public void AddFilter(QueryToken queryToken)
        {
            if (queryToken == null)
            {
                MessageBox.Show(Properties.Resources.NoColumnSelected);
                return;
            }

            FilterType ft = QueryUtils.GetFilterType(queryToken.Type);

            FilterOption f = new FilterOption
            {
                Token = queryToken,
                Value = queryToken.Type.IsValueType && !queryToken.Type.IsNullable() ? Activator.CreateInstance(queryToken.Type) : null,
                Operation = QueryUtils.GetFilterOperations(ft).FirstEx()
            };

            Filters.Add(f);
        }
    }
}
