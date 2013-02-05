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
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for EntityTitle.xaml
    /// </summary>
    public partial class EntityTitle : UserControl
    {
        public EntityTitle()
        {
            InitializeComponent();
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(EntityTitle_DataContextChanged);
        }

        void EntityTitle_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            tbEntityId.Text = GetTitle(e.NewValue as ModifiableEntity);
        }

        string GetTitle(ModifiableEntity mod)
        {
            if (mod == null)
                return "";
            
            string niceName = mod.GetType().NiceName();

            IdentifiableEntity ident = mod as IdentifiableEntity;
            if (ident == null)
                return niceName;

            if (ident.IsNew)
            {
                return LiteMessage.New.NiceToString().ForGenderAndNumber(ident.GetType().GetGender()) + " " + niceName; 
            }
            return niceName + " " + ident.Id;
        }

        public void SetTitleText(string text)
        {
            tbEntityId.Text = text;
        }
    }
}
