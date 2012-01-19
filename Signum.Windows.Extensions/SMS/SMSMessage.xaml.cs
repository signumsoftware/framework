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
using Signum.Entities.SMS;

namespace Signum.Windows.SMS
{
    /// <summary>
    /// Interaction logic for SMSMessage.xaml
    /// </summary>
    public partial class SMSMessage : UserControl
    {
        public static IValueConverter StateToVisibility = ConverterFactory.New((SMSMessageState s) => s == SMSMessageState.Created ? Visibility.Visible : Visibility.Collapsed);
        public static IValueConverter NotStateToVisibility = ConverterFactory.New((SMSMessageState s) => s != SMSMessageState.Created ? Visibility.Visible : Visibility.Collapsed);

        public SMSMessage()
        {
            InitializeComponent();
            text = textMessage.Child<TextBox>();
            text.TextChanged += new TextChangedEventHandler(SMSMessage_TextChanged);
            BrushConverter bc = new BrushConverter();
            greenBrush = (Brush)bc.ConvertFromString("Green");
            redBrush = (Brush)bc.ConvertFromString("Red");
            pinkBackground = new LinearGradientBrush(new GradientStopCollection 
            { 
                new GradientStop(Color.FromRgb(255, 237, 237), 0),
                new GradientStop(Color.FromRgb(255, 231, 231), 0.527),
                new GradientStop(Color.FromRgb(255, 204, 204), 1)
            }, new Point(0.5, 1), new Point(0.5, 0));
            normalBackGround = text.Background;
        }

        private void SMSMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            VisualCharactersToEnd();
        }

        LinearGradientBrush pinkBackground = null;
        TextBox text = null;
        Brush greenBrush = null;
        Brush redBrush = null;
        Brush normalBackGround = null;

        private void VisualCharactersToEnd()
        {
            int chLeft = SMSCharacters.RemainingLength(text.Text);
            charactersLeft.Text = chLeft.ToString();
            if (chLeft < 0)
            {
                charactersLeft.Foreground = redBrush;
                charactersLeft.Background = pinkBackground;
            }
            else
            {
                charactersLeft.Foreground = greenBrush;
                charactersLeft.Background = normalBackGround;
            }
        }

        public SMSMessageDN MessageDC
        {
            get { return (SMSMessageDN)DataContext; }
            set { RaiseEvent(new ChangeDataContextEventArgs(value)); }
        }

        private void removeNonSMSChars_Click(object sender, RoutedEventArgs e)
        {
            MessageDC.Message = SMSCharacters.RemoveNoSMSCharacters(MessageDC.Message);
            MessageDC = MessageDC;
        }
    }
}
