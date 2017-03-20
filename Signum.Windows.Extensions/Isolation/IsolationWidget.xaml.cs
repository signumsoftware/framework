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
using Signum.Entities.Isolation;
using Signum.Windows;

namespace Signum.Windows.Isolation
{
    public partial class IsolationWidget : UserControl, IWidget
    {
        public event Action ForceShow;

        public IsolationWidget()
        {
            InitializeComponent();
            this.DataContextChanged += EntidadSistemaWidget_DataContextChanged;
        }

        void EntidadSistemaWidget_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

            if (e.NewValue is Entity ident)
            {
                var isolation = ident.Isolation();

                if (isolation == null)
                    this.Visibility = System.Windows.Visibility.Collapsed;
                else
                {
                    if (IsolationEntity.Default == null && !IsolationEntity.Default.Is(isolation))
                    {
                        ForceShow?.Invoke();
                    }

                    img.Source = IsolationClient.GetIsolationIcon(isolation);

                    tb.Text = img.Source != null ? null :
                        isolation.ToString();
                }
            }

        }
    }
}
