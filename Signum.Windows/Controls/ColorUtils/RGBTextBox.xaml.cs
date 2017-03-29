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

namespace Signum.Windows.ColorUtils
{
    /// <summary>
    /// Interaction logic for RGBTextBox.xaml
    /// </summary>
    public partial class RGBTextBox : UserControl
    {
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(RGBTextBox),
            new FrameworkPropertyMetadata(default(Color), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((RGBTextBox)d).ColorChanged(e)));


        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        public RGBTextBox()
        {
            InitializeComponent();
            
        }

        private void PART_tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender; 
            string txt = tb.Text;
            if (byte.TryParse(txt, out byte b))
            {
                Color = Color.FromArgb(
                    tb == PART_tbA ? b : Color.A,
                    tb == PART_tbR ? b : Color.R,
                    tb == PART_tbG ? b : Color.G,
                    tb == PART_tbB ? b : Color.B);
            }
        }

        private void ColorChanged(DependencyPropertyChangedEventArgs e)
        {
            Color color = (Color)e.NewValue;
            PART_tbA.Text = color.A.ToString();
            PART_tbR.Text = color.R.ToString();
            PART_tbG.Text = color.G.ToString();
            PART_tbB.Text = color.B.ToString(); 
        }

        //protected override void OnGotFocus(RoutedEventArgs e)
        //{
        //    PART_tbA.Focus();
        //}

        
    }
}
