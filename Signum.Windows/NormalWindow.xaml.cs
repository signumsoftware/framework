using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Signum.Entities;
using System.Windows.Threading;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using System.Linq;
using System.Collections.Generic;
using Signum.Entities.Reflection;
using System.Windows.Input;

namespace Signum.Windows
{
	public partial class NormalWindow
	{
        public static readonly DependencyProperty MainControlProperty =
            DependencyProperty.Register("MainControl", typeof(Control), typeof(NormalWindow));
        public Control MainControl
        {
            get { return (Control)GetValue(MainControlProperty); }
            set { SetValue(MainControlProperty, value); }
        }

        public ButtonBar ButtonBar
        {
            get { return this.buttonBar; }
        }

		public NormalWindow()
		{
			this.InitializeComponent();

            this.DataContextChanged+=new DependencyPropertyChangedEventHandler(NormalWindow_DataContextChanged);

            Common.AddChangeDataContextHandler(this, ChangeDataContext_DataContextChanged);

            RefreshEnabled();
  
            this.Loaded += new RoutedEventHandler(NormalWindow_Loaded);
		}

        void ChangeDataContext_DataContextChanged(object sender, ChangeDataContextEventArgs e)
        {
            DataContext = null; 
            DataContext = e.NewDataContext;
            e.Handled = true; 
        }

        void NormalWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.buttonBar.SaveButton.IsEnabled = !Common.GetIsReadOnly(this);
        }

        void NormalWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
             RefreshEnabled();
        }

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.S && (e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0 && buttonBar.SaveVisible)
            {
                Save(); 
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            base.DialogResult = true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Save()
        {
            if (!this.HasChanges())
            {
                MessageBox.Show(Properties.Resources.NoChanges);

                return;
            }

            if (!this.AssertErrors())
                return;

            buttonBar.SaveButton.IsEnabled = false;
            IdentifiableEntity ei = (IdentifiableEntity)base.DataContext;
            IdentifiableEntity nueva = null;
            Async.Do(this,
                () => nueva = Server.Save(ei),
                () => { base.DataContext = null; base.DataContext = nueva; },
                () => buttonBar.SaveButton.IsEnabled = true);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            if (buttonBar.SaveVisible && this.HasChanges())
            {
                var result = MessageBox.Show(Properties.Resources.SaveChanges, Properties.Resources.ThereAreChanges,
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No);

                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == MessageBoxResult.Yes)
                    DataContext = Server.Save((IdentifiableEntity)DataContext);
            }
        }

        void RefreshEnabled()
        {
            buttonBar.ReloadButton.IsEnabled = (DataContext as IdentifiableEntity).TryCS(ei => !ei.IsNew) ?? false; 
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            if (this.LooseChangesIfAny())
            {
                IdentifiableEntity ei = (IdentifiableEntity)DataContext;
                DataContext = null;  // Equal returns true 
                DataContext = Server.Retrieve(ei.GetType(), ei.Id);
            }
        }

        private void widgetPanel_ExpandedCollapsed(object sender, RoutedEventArgs e)
        {
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }
	}
}