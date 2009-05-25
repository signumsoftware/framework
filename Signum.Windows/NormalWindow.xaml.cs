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
        
        public static readonly DependencyProperty ButtonsProperty =
            DependencyProperty.Register("Buttons", typeof(ViewButtons), typeof(NormalWindow), new FrameworkPropertyMetadata(ViewButtons.OkCancel, FrameworkPropertyMetadataOptions.None, new PropertyChangedCallback(ButtonsChanged)));
        public ViewButtons Buttons
        {
            get { return (ViewButtons)GetValue(ButtonsProperty); }
            set { SetValue(ButtonsProperty, value); }
        }

		public NormalWindow()
		{
			this.InitializeComponent();

            OnButtonsChanged();
            this.DataContextChanged+=new DependencyPropertyChangedEventHandler(NormalWindow_DataContextChanged);
            RefreshEnabled();

            this.Loaded += new RoutedEventHandler(NormalWindow_Loaded);
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
            if (e.Key == Key.S && (e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0 && Buttons == ViewButtons.Save)
            {
                Save(); 
            }
        }

        public static void ButtonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((NormalWindow) d).OnButtonsChanged();
        }

        private void OnButtonsChanged()
        {
            ViewButtons b = this.Buttons;
            buttonBar.OkVisible = b == ViewButtons.OkCancel;
            buttonBar.CancelVisible = b == ViewButtons.OkCancel;
            buttonBar.SaveVisible = b == ViewButtons.Save;
        }

        //private void OkSaving_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!AssertErrors(Graph()))
        //        return;

        //    buttonBar.SaveButton.IsEnabled = false;
        //    IdentifiableEntity ei = (IdentifiableEntity)base.DataContext;
        //    IdentifiableEntity nueva = null;
        //    Async.Do(this,
        //        () => nueva = Server.Save(ei),
        //        () => { base.DataContext = null; base.DataContext = nueva; base.DialogResult = true; this.Close(); },
        //        () => buttonBar.SaveButton.IsEnabled = true);       
        //}

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            base.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            base.DialogResult = false;
        }


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Save()
        {
            var graph = Graph();
            if (!HasChanges(graph))
            {
                MessageBox.Show(Properties.Resources.NoChanges);

                return;
            }

            if (!AssertErrors(graph))
                return;

            buttonBar.SaveButton.IsEnabled = false;
            IdentifiableEntity ei = (IdentifiableEntity)base.DataContext;
            IdentifiableEntity nueva = null;
            Async.Do(this,
                () => nueva = Server.Save(ei),
                () => { base.DataContext = null; base.DataContext = nueva; },
                () => buttonBar.SaveButton.IsEnabled = true);
        }

        public bool AssertErrors(DirectedGraph<Modifiable> graph)
        {
            GraphExplorer.PreSaving(graph);
            string error = GraphExplorer.Integrity(graph);

            if (error.HasText())
            {
                MessageBox.Show(Properties.Resources.ImpossibleToSaveIntegrityCheckFailed + error, Properties.Resources.ThereAreErrors, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        public DirectedGraph<Modifiable> Graph()
        {
            return GraphExplorer.FromRoot((IdentifiableEntity)base.DataContext); 
        }

        bool HasChanges(DirectedGraph<Modifiable> graph)
        {
            return graph.Any(a => a.SelfModified);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            if (Buttons == ViewButtons.Save && HasChanges(Graph()))
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
            if (!HasChanges(Graph()) || MessageBox.Show(Properties.Resources.ThereAreChangesContinue, Properties.Resources.ThereAreChanges,
                MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK) == MessageBoxResult.OK)
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