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
using Signum.Entities;
using System.Collections;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Threading;
using System.Threading;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class EntityLine : EntityBase
    {
        public event Func<string, IEnumerable<Lite<Entity>>> Autocompleting;

        public static readonly DependencyProperty AutocompleteProperty =
            DependencyProperty.Register("Autocomplete", typeof(bool), typeof(EntityLine), new FrameworkPropertyMetadata(true));
        public bool Autocomplete
        {
            get { return (bool)GetValue(AutocompleteProperty); }
            set { SetValue(AutocompleteProperty, value); }
        }

        int autocompleteElements = 5;
        public int AutocompleteElements
        {
            get { return autocompleteElements; }
            set { autocompleteElements = value; }
        }

        public EntityLine()
        {
            InitializeComponent();
        }

        public override void OnLoad(object sender, RoutedEventArgs e)
        {
            base.OnLoad(sender, e);

            if (Implementations == null || Implementations.Value.IsByAll)
                Autocomplete = false;
        }

        protected override void UpdateVisibility()
        {
            btCreate.Visibility = CanCreate() ? Visibility.Visible : Visibility.Collapsed;
            btFind.Visibility = CanFind() ? Visibility.Visible : Visibility.Collapsed;
            btView.Visibility = CanViewOrNavigate() ? Visibility.Visible : Visibility.Collapsed;
            btRemove.Visibility = CanRemove() ? Visibility.Visible : Visibility.Collapsed;
        }

        public bool CanAutocomplete()
        {
            return !Common.GetIsReadOnly(this) && Autocomplete;
        }

        private IEnumerable autocompleteTextBox_Autocompleting(string arg, CancellationToken ct)
        {
            IEnumerable value;
            if (Autocompleting != null)
                value = Autocompleting(arg);
            else
                value = Server.FindLiteLike(safeImplementations.Value, arg, AutocompleteElements);

            return value;
        }

        private void autocompleteTextBox_SelectedItemChanged(object sender, RoutedEventArgs e)
        {
            autocompleteTextBox.Visibility = Visibility.Hidden;
            cc.Focus();
        }

        private void autocompleteTextBox_Closed(object sender, CloseEventArgs e)
        {
            if (e.IsCommit)
            {
                if (CanAutocomplete())
                    SetEntityUserInteraction(Server.Convert(autocompleteTextBox.SelectedItem, Type));

                autocompleteTextBox.Visibility = Visibility.Hidden;
                cc.Focus();
            }
            else
            {
                if (e.Reason != CloseReason.LostFocus)
                    autocompleteTextBox.Visibility = Visibility.Hidden;
            }
        }

        public void ActivateAutocomplete()
        {
            if (CanAutocomplete() && autocompleteTextBox.Visibility != Visibility.Visible)
            {
                autocompleteTextBox.Visibility = Visibility.Visible;
                autocompleteTextBox.Text = Entity?.ToString();
                autocompleteTextBox.SelectAndFocus();
            }
        }

        private void cc_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                ActivateAutocomplete();
                e.Handled = true;
            }
        }

        bool doubleClicked = false;
        private void cc_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            doubleClicked = true; 
            ActivateAutocomplete();
        }

        private void cc_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Entity == null)
            {
                ActivateAutocomplete();
                e.Handled = true;
            }
        }

        private void cc_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (doubleClicked)
            {
                doubleClicked = false;
                return;
            }

            if (!cc.Focus())
                ActivateAutocomplete();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new EntityLinePeer(this);
        }

    }

    public class EntityLinePeer : UserControlAutomationPeer, IInvokeProvider
    {
        public EntityLinePeer(EntityLine el)
            : base(el)
        {
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Invoke)
                return this;

            return base.GetPattern(patternInterface);
        }

        public void Invoke()
        {
            if (!base.IsEnabled() || !((EntityLine)Owner).Autocomplete)
                throw new InvalidOperationException("Autocomplete not enabled");

            base.Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(obj =>
            {
                ((EntityLine)base.Owner).ActivateAutocomplete();
                return null;
            }), null);

        }
    }
}
