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
using Signum.Entities.Translation;

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
        }

        private void EntityCombo_EntityChanged(object sender, bool userInteraction, object oldValue, object newValue)
        {
            sfLiterals.Items.Clear();
            if (newValue != null)
            {
                var literals = Server.Return((ISmsServer s) => s.GetLiteralsFromDataObjectProvider((TypeEntity)newValue));
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

                var msgCtrl = tabCulture.Child<SMSTemplateMessage>(WhereFlags.VisualTree);

                msgCtrl.ReplaceText(literal);
            }
        }

        private IEnumerable<Lite<IEntity>> EntityCombo_LoadData()
        {
            return Server.Return((ISmsServer s) => s.GetAssociatedTypesForTemplates());
        }

        private void deleteMessage_Click(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            SMSTemplateMessageEntity message = (SMSTemplateMessageEntity)b.DataContext;
            ((SMSTemplateEntity)DataContext).Messages.Remove(message);
        }

        static CultureInfoEntity defaultCulture;
        static CultureInfoEntity DefaultCulture
        {
            get 
            {
                if (defaultCulture == null)
                {
                    defaultCulture = Server.Return((ISmsServer s) => s.GetDefaultCulture());
                }
                return defaultCulture;
            }
        }

        private void createMessage_Click(object sender, RoutedEventArgs e)
        {
            ((SMSTemplateEntity)DataContext).Messages.Add(new SMSTemplateMessageEntity(DefaultCulture));
            tabCulture.SelectedIndex = ((SMSTemplateEntity)DataContext).Messages.Count - 1;

        }
    }
}
