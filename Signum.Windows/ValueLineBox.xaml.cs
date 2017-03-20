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
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using Signum.Entities;
using Signum.Utilities;
using System.Windows.Automation.Peers;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for TypeSelectorWindow.xaml
    /// </summary>
    public partial class ValueLineBox : Window
    {

        public ValueLineBox()
        {
            InitializeComponent();        
        }


        private void btAccept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close(); 
        }

        public static bool Show<T>(ref T value, string title = null, string text = null, 
            string labelText = null, string format = null, string unitText = null, 
            Window owner = null)
        {
            object obj = value;
            if (ShowUntyped(typeof(T), ref obj, title, text, labelText, format, unitText, owner))
            {
                value = (T)obj;
                return true;
            }
            return false;
        }

        public static bool ShowUntyped(Type type, ref object value, string title = null, string text = null, 
            string labelText = null, string format = null, string unitText = null, 
            Window owner = null)
        {
            ValueLineBox vlb = new ValueLineBox()
            {
                Title = title ?? SelectorMessage.ChooseAValue.NiceToString()
            };
            vlb.tb.Text = text ?? SelectorMessage.PleaseChooseAValueToContinue.NiceToString();

            vlb.valueLine.Type = type;

            if (labelText == null)
                Common.SetLabelVisible(vlb.valueLine, false);
            else
                vlb.valueLine.LabelText = labelText;

            vlb.valueLine.Format = format;
            vlb.valueLine.UnitText = unitText;
            vlb.valueLine.Value = value;

            vlb.Owner = owner;

            if (vlb.ShowDialog() == true)
            {
                value = vlb.valueLine.Value;
                return true;
            }
            return false;
        }

        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close(); 
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ValueLineBoxAutomationPeer(this);
        }
    }

    public class ValueLineBoxAutomationPeer : WindowAutomationPeer
    {
        public ValueLineBoxAutomationPeer(ValueLineBox valueLineBox)
            : base(valueLineBox)
        {
        }

        protected override string GetClassNameCore()
        {
            return "ValueLineBox";
        }
    }

}
