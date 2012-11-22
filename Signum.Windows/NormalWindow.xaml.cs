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

        public static readonly DependencyProperty AllowErrorsProperty =
            DependencyProperty.Register("AllowErrors", typeof(AllowErrors), typeof(NormalWindow), new UIPropertyMetadata(AllowErrors.Ask));
        public AllowErrors AllowErrors
        {
            get { return (AllowErrors)GetValue(AllowErrorsProperty); }
            set { SetValue(AllowErrorsProperty, value); }
        }

        public ButtonBar ButtonBar
        {
            get { return this.buttonBar; }
        }

        public NormalWindow()
        {
            this.InitializeComponent();

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(NormalWindow_DataContextChanged);

            Common.AddChangeDataContextHandler(this, ChangeDataContext_Handler);

            RefreshEnabled();

            this.Loaded += new RoutedEventHandler(NormalWindow_Loaded);
        }

        void ChangeDataContext_Handler(object sender, ChangeDataContextEventArgs e)
        {
            if (e.Refresh)
            {
                var l = ((IdentifiableEntity)this.DataContext).ToLite().Retrieve();
                this.DataContext = null;
                this.DataContext = l;
                e.Handled = true;
            }
            else
            {
                if (e.NewDataContext == null)
                    throw new ArgumentNullException("NewDataContext");

                Type type = Common.GetTypeContext(MainControl).Type;
                Type entityType = e.NewDataContext.GetType();
                if (type != null && !type.IsAssignableFrom(entityType))
                    throw new InvalidCastException("The DataContext is a {0} but TypeContext is {1}".Formato(entityType.Name, type.Name));

                DataContext = null;
                DataContext = e.NewDataContext;
                e.Handled = true;
            }
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
            if (Navigator.Manager.CanSave(this.DataContext.GetType()))
            {
                string errors = this.GetErrors();

                if (errors.HasText())
                {
                    Type type = DataContext.GetType();

                    switch (AllowErrors)
                    {
                        case AllowErrors.Yes: break;
                        case AllowErrors.No:
                            MessageBox.Show(this, 
                                type.GetGenderAwareResource(() => Properties.Resources.The0HasErrors1).Formato(type.NiceName(), errors.Indent(3)), 
                                Properties.Resources.FixErrors,
                                MessageBoxButton.OK, 
                                MessageBoxImage.Exclamation);
                            return;
                        case AllowErrors.Ask:
                            if (MessageBox.Show(this, 
                                type.GetGenderAwareResource(() => Properties.Resources.The0HasErrors1).Formato(type.NiceName(), errors.Indent(3)) + "\r\n" + Properties.Resources.ContinueAnyway, 
                                Properties.Resources.ContinueWithErrors,
                                MessageBoxButton.YesNo, 
                                MessageBoxImage.Exclamation, 
                                MessageBoxResult.None) == MessageBoxResult.No)
                                return;
                            break;
                    }
                }

                base.DialogResult = true;
            }
            else
            {
                if (!this.HasChanges())
                    DialogResult = true;
                else
                {
                    var result = MessageBox.Show(
                        Properties.Resources.ThereAreChangesContinue,
                        Properties.Resources.ThereAreChanges,
                        MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK);

                    if (result == MessageBoxResult.Cancel)
                        return;

                    DialogResult = false;

                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Save()
        {
            if (!this.HasChanges())
            {
                MessageBox.Show(this, Properties.Resources.NoChanges);

                return;
            }

            if (!MainControl.AssertErrors())
                return;

            buttonBar.SaveButton.IsEnabled = false;
            IdentifiableEntity ei = (IdentifiableEntity)base.DataContext;
            IdentifiableEntity nueva = null;
            Async.Do(
                () => nueva = Server.Save(ei),
                () => { base.DataContext = null; base.DataContext = nueva; },
                () => buttonBar.SaveButton.IsEnabled = true);
        }


        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            MoveFocus();

            if (this.HasChanges())
            {
                if (buttonBar.SaveVisible)
                {
                    if (Navigator.Manager.CanSave(this.DataContext.GetType()))
                    {
                        var result = MessageBox.Show(this,
                            Properties.Resources.SaveChanges,
                            Properties.Resources.ThereAreChanges,
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question,
                            MessageBoxResult.No);

                        if (result == MessageBoxResult.Cancel)
                        {
                            e.Cancel = true;
                            return;
                        }

                        if (result == MessageBoxResult.Yes)
                            DataContext = Server.Save((IdentifiableEntity)DataContext);
                    }
                    else
                    {
                        var result = MessageBox.Show(
                          Properties.Resources.ThereAreChangesContinue,
                          Properties.Resources.ThereAreChanges,
                          MessageBoxButton.OKCancel,
                          MessageBoxImage.Question,
                          MessageBoxResult.OK);

                        if (result == MessageBoxResult.Cancel)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }
                else
                {
                    if (DialogResult == null)
                    {
                        var result = MessageBox.Show(this, 
                            Properties.Resources.LoseChanges, 
                            Properties.Resources.ThereAreChanges, 
                            MessageBoxButton.OKCancel, 
                            MessageBoxImage.Question, 
                            MessageBoxResult.OK);

                        if (result == MessageBoxResult.Cancel)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }
            }
        }

        private static void MoveFocus()
        {
            // Change keyboard focus.
            UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;

            if (elementWithFocus != null)
            {
                elementWithFocus.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
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

        public void SetTitleText(string text)
        {
            this.entityTitle.SetTitleText(text);
        }

        public ChangeDataContextHandler ChangeDataContext { get; set; }
    }

    public enum AllowErrors
    {
        Ask,
        Yes, 
        No,
    }
}
