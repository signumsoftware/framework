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
using System.Diagnostics;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System.Reflection;
using System.ComponentModel;
using Signum.Utilities.DataStructures;
using System.Collections;
using System.Windows.Controls.Primitives;
using System.Globalization;
using Signum.Entities.Basics;

namespace Signum.Windows
{
    

    /// <summary>
    /// Utiliza una deduccion de propiedades muy agresiva:
    /// Value (binding) -> ValueType -> ValueLineType -> ValueControl
    /// </summary>
    public partial class TextArea : LineBase
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(object), typeof(TextArea), new UIPropertyMetadata(null));
        public object Text
        {
            get { return (object)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        static TextArea()
        {
            Common.ValuePropertySelector.SetDefinition(typeof(TextArea), TextProperty);
        }

        public TextArea()
        {
            InitializeComponent();
        }

        public override void OnLoad(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            base.OnLoad(sender, e);

            if (this.Type != typeof(string))
                throw new InvalidOperationException("Type for TextArea {0} should be string".Formato(this.LabelText));
        }
    }
}
