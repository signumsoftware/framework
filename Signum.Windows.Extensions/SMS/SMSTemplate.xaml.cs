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
using Signum.Entities.Basics;

namespace Signum.Windows.SMS
{
    /// <summary>
    /// Interaction logic for SMSTemplate.xaml
    /// </summary>
    public partial class SMSTemplate : UserControl
    {
        public SMSTemplate()
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
            VisualCharactersToEnd();
        }

        public SMSTemplateDN TemplateDC
        {
            get { return (SMSTemplateDN)DataContext; }
            set { RaiseEvent(new ChangeDataContextEventArgs(value)); }
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

        private void removeNonSMSChars_Click(object sender, RoutedEventArgs e)
        {
            TemplateDC.Message = SMSCharacters.RemoveNoSMSCharacters(TemplateDC.Message);
            TemplateDC = TemplateDC;
        }

        private void EntityCombo_EntityChanged(object sender, bool userInteraction, object oldValue, object newValue)
        {
            sfLiterals.Items.Clear();
            if (newValue != null)
            {
                var literals = Server.Return((ISmsServer s) => s.GetLiteralsFromDataObjectProvider((TypeDN)newValue));
                foreach (var l in literals)
                {
                    sfLiterals.Items.Add(l);
                }
            }
        }

        private void insertLiteral_Click(object sender, RoutedEventArgs e)
        {
            InsertSelectedLiteral();
        }

        private void sfLiterals_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            InsertSelectedLiteral();
        }

        private void InsertSelectedLiteral()
        {
            if (sfLiterals.SelectedItem == null)
                MessageBox.Show("", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            else
            {
                string literal = (string)sfLiterals.SelectedItem;
                text.SelectedText = literal;
                text.SelectionStart = text.SelectionStart + literal.Length;
                text.SelectionLength = 0;
            }
        }

        private IEnumerable<Lite<IIdentifiable>> EntityCombo_LoadData()
        {
            return Server.Return((ISmsServer s) => s.GetAssociatedTypesForTemplates());
        }
    }
}
