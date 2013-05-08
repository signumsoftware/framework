using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;

namespace Signum.Windows
{
    public class DesignTimeResourceDictionary : ResourceDictionary
    {
        public bool IsInDesignMode
        {
            get
            {
                return (bool)DependencyPropertyDescriptor.FromProperty(DesignerProperties.IsInDesignModeProperty,typeof(DependencyObject)).Metadata.DefaultValue;
            }
        }

        public new Uri Source
        {
            get { return base.Source; }
            set
            {
                if (!IsInDesignMode)
                    return;

                Debug.WriteLine("Setting Source = " + value);
                base.Source = value;
            }
        }
    }
}
