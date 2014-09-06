using Signum.Entities;
using Signum.Entities.SMS;
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

namespace Signum.Windows.SMS
{
    /// <summary>
    /// Interaction logic for SMSTemplateMessage.xaml
    /// </summary>
    public partial class SMSTemplateMessage : UserControl
    {
        public SMSTemplateMessage()
        {
            InitializeComponent();
            InitialSet();
        }

        LinearGradientBrush pinkBackground = null;
        TextBox text = null;
        Brush greenBrush = null;
        Brush redBrush = null;
        Brush normalBackGround = null;

        void InitialSet()
        {
            text = textMessage.Child<TextBox>();
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
            VisualCharactersToEnd();
        }

        private void SMSMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            VisualCharactersToEnd();
        }

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

        public SMSTemplateMessageDN TemplateMessageDC
        {
            get { return (SMSTemplateMessageDN)DataContext; }
            set { RaiseEvent(new ChangeDataContextEventArgs(value)); }
        }

        private void removeNonSMSChars_Click(object sender, RoutedEventArgs e)
        {
            TemplateMessageDC.Message = SMSCharacters.RemoveNoSMSCharacters(TemplateMessageDC.Message);
            TemplateMessageDC = TemplateMessageDC;
        }

        internal void ReplaceText(string newText)
        {
            text.SelectedText = newText;
            text.SelectionStart = text.SelectionStart + newText.Length;
            text.SelectionLength = 0;
        }
    }
}
