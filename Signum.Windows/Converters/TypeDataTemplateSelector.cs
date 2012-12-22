using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows;
using Signum.Utilities;
using System.Collections;
using System.Collections.ObjectModel;

namespace Signum.Windows
{
    [ContentProperty("Templates")]
    public class TypeDataTemplateSelector : DataTemplateSelector
    {
        DataTemplateCollection templates = new DataTemplateCollection();
        public DataTemplateCollection Templates
        {
            get { return templates; }
            set { templates = value; }
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
                return null;

            return templates.SingleOrDefaultEx(t => t.DataType as Type == item.GetType());
        }
    }

    public class DataTemplateCollection : Collection<DataTemplate> { }
}
