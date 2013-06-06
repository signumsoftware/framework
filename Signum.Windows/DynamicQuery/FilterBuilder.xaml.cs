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

        public FilterBuilder()
        {
            this.InitializeComponent();
        }

        private void filterRemove_Click(object sender, EventArgs e)
        {
            Filters.Remove((FilterOption)((FilterLine)sender).DataContext);
        }

        public void AddFilter(QueryToken queryToken)
        {
            if (queryToken == null)
            {
                MessageBox.Show(SearchMessage.NoColumnSelected.NiceToString());
                return;
            }

            FilterOption f = new FilterOption
            {
                Token = queryToken,
                Value = queryToken.Type.IsValueType && !queryToken.Type.IsNullable() ? Activator.CreateInstance(queryToken.Type) : null,
                Operation = FilterOperation.EqualTo
            };

            Filters.Add(f);
        }
    }
}
