using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Collections;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading;
using System.Windows.Media;
using Signum.Utilities;
using System.Windows.Automation.Peers;
using System.Collections.Generic;
using System.Windows.Automation.Provider;
using System.Threading.Tasks;

namespace Signum.Windows
{
    //http://www.lazarciuc.ro/ioan/2008/06/01/auto-complete-for-textboxes-in-wpf/
    public partial class AutocompleteTextBox : UserControl
    {
        public static readonly RoutedEvent ClosedEvent =
            EventManager.RegisterRoutedEvent("Closed", RoutingStrategy.Bubble, typeof(ClosedEventHandler), typeof(AutocompleteTextBox));
        public event ClosedEventHandler Closed
        {
            add { AddHandler(ClosedEvent, value); }
            remove { RemoveHandler(ClosedEvent, value); }
        }

        public event Func<string, CancellationToken, IEnumerable> Autocompleting;

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(AutocompleteTextBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (s, o) => ((AutocompleteTextBox)s).txtBox.Text = o.NewValue?.ToString()));
        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty MinTypedCharactersProperty =
            DependencyProperty.Register("MinTypedCharacters", typeof(int), typeof(AutocompleteTextBox), new UIPropertyMetadata(1));
        public int MinTypedCharacters
        {
            get { return (int)GetValue(MinTypedCharactersProperty); }
            set { SetValue(MinTypedCharactersProperty, value); }
        }


        public static readonly DependencyProperty AllowFreeTextProperty =
            DependencyProperty.Register("AllowFreeText", typeof(bool), typeof(AutocompleteTextBox), new UIPropertyMetadata(false));
        public bool AllowFreeText
        {
            get { return (bool)GetValue(AllowFreeTextProperty); }
            set { SetValue(AllowFreeTextProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(AutocompleteTextBox), new UIPropertyMetadata(null));
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateSelectorProperty =
           DependencyProperty.Register("ItemTemplateSelector", typeof(DataTemplateSelector), typeof(AutocompleteTextBox), new UIPropertyMetadata(null));
        public DataTemplateSelector ItemTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty); }
            set { SetValue(ItemTemplateSelectorProperty, value); }
        }

        DispatcherTimer delayTimer = new DispatcherTimer(DispatcherPriority.Normal);

        public TimeSpan Delay
        {
            get { return delayTimer.Interval; }
            set { delayTimer.Interval = value; }
        }
        
        public AutocompleteTextBox()
        {
            InitializeComponent();
            delayTimer.Interval = TimeSpan.FromMilliseconds(300);
            itemsSelected = false;
            delayTimer.Tick += new EventHandler(delayTimer_Tick);
        }

        CancellationTokenSource source;

        internal void delayTimer_Tick(object sender, EventArgs e)
        {
            delayTimer.Stop();

            if (Autocompleting == null) 
                throw new NullReferenceException("SeachMethod cannot be null.");

            string text = txtBox.Text;

            source = new CancellationTokenSource();
            var context = Statics.ExportThreadContext();
            var task = Task.Factory.StartNew<IEnumerable>(() =>
            {
                Statics.ImportThreadContext(context);
                return Autocompleting(text, source.Token);
            }, source.Token);

            task.ContinueWith(res =>
            {
                if (res.IsFaulted)
                    Async.OnAsyncUnhandledException(res.Exception.InnerExceptions.FirstEx(), Window.GetWindow(this));
                else
                {
                    lstBox.ItemsSource = res.Result;
                    
                    if (lstBox.Items.Count > 0)
                    {
                        lstBox.SelectedIndex = -1;
                        pop.Width = lstBox.Width;
                        pop.Height = lstBox.Height;
                        pop.IsOpen = true;
                    }
                    else
                    {
                        pop.IsOpen = false;
                    }
                }
            }, source.Token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext()); 
        }

        void MoveDown()
        {
            if (lstBox.SelectedIndex < lstBox.Items.Count - 1)
            {
                lstBox.SelectedIndex++;
            }
        }

        void MoveUp()
        {
            if (lstBox.SelectedIndex > 0)
            {
                lstBox.SelectedIndex--;
            }
        }
    
        void txtBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                MoveUp();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                MoveDown();
                e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                if (lstBox.SelectedItem != null)
                    e.Handled = Commit(CloseReason.Tab);
                else if (lstBox.Items.Count == 1)
                {
                    MoveDown();
                    e.Handled = Commit(CloseReason.Tab);
                }
                else
                    e.Handled = Close(CloseReason.TabExit);
                
                //e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if(lstBox.SelectedItem != null)
                    Commit(CloseReason.Enter);
                else if (lstBox.Items.Count == 1)
                {
                    MoveDown();
                    Commit(CloseReason.Enter);
                }
                
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Close(CloseReason.Esc);
                e.Handled = true;
            }
           
        }

        void txtBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (IsTextChangingKey(e.Key))
            {
                Suggest();
            }
        }


        bool IsTextChangingKey(Key key)
        {
            if (key == Key.Space || key == Key.Delete || key == Key.Back)
                return true;

            if (Key.A <= key && key <= Key.Z)
                return true;

            if (Key.D0 <= key && key <= Key.D9)
                return true;
            
            if (Key.NumPad0 <= key && key <= Key.NumPad9)
                return true;

            if (Key.Multiply <= key && key <= Key.Divide)
                return true;

            if (Key.OemSemicolon <= key && key <= Key.Oem102)
                return true;

            return false;
        }

        public bool Close(CloseReason reason)
        {
            pop.IsOpen = false;
            if (SelectedItem?.ToString() != txtBox.Text)
            {
                if (string.IsNullOrEmpty(txtBox.Text))
                    SelectedItem = null;
                else if (AllowFreeText)
                    SelectedItem = txtBox.Text;
            }
            var args = new CloseEventArgs(reason);
            RaiseEvent(args);
            return args.Handled;
        }


        bool Commit(CloseReason reason)
        {
            pop.IsOpen = false;
            if (lstBox.SelectedItem != null)
            {
                SelectedItem = lstBox.SelectedItem;

                var result = Close(reason); 

                itemsSelected = true;

                return result;
            }

            return false;
        }

        private void txtBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (itemsSelected)
                itemsSelected = false;
            else Suggest();
        }

        private void txtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!pop.IsKeyboardFocusWithin)
            {
                if (string.IsNullOrEmpty(this.txtBox.Text))
                    SelectedItem = null;

                Close(CloseReason.LostFocus);
            }
        }

        private void Suggest()
        {
            if (txtBox.Text.Length < MinTypedCharacters)
            {
                pop.IsOpen = false;
                lstBox.ItemsSource = null;
                return;
            }

            var s = source;

            if (s != null)
                s.Cancel();

            delayTimer.Start(); 
        }

        private bool itemsSelected;
        private void lstBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pop.IsKeyboardFocusWithin)
            {
                Commit(CloseReason.ClickList);
            }
        }

        public void SelectAndFocus()
        {
            txtBox.SelectAll();
            txtBox.Focus();
            Mouse.Capture(this, CaptureMode.SubTree); 
        }

        public void SelectEnd()
        {
            txtBox.Focus();
            txtBox.Select(txtBox.Text.Length, 0);
        }

        public string Text
        {
            get { return txtBox.Text; }
            set { txtBox.Text = value; }
        }

        private void userControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Console.WriteLine("Down");
            if (!txtBox.IsMouseOver && !pop.IsMouseOver)
            {
                ReleaseMouseCapture();
                Close(CloseReason.ClickOut);
            }
            else if (pop.IsMouseOver)
            {
                var sb = lstBox.Child<ScrollBar>(WhereFlags.VisualTree);
                if (!sb.IsMouseOver)
                {
                    ReleaseMouseCapture();
                    Commit(CloseReason.ClickList);
                }
            }

        }

        //private void me_GotMouseCapture(object sender, MouseEventArgs e)
        //{
        //    Console.WriteLine("Got");
        //}

        //private void me_LostMouseCapture(object sender, MouseEventArgs e)
        //{
        //    Console.WriteLine("Lost");
        //}

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new AutocompleteAutomationPeer(this);
        }

    }

    public class AutocompleteAutomationPeer : UserControlAutomationPeer, IValueProvider
    {
        public AutocompleteAutomationPeer(AutocompleteTextBox ac)
            : base(ac)
        {
        }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> childrenCore = new List<AutomationPeer>();
            AutocompleteTextBox owner = (AutocompleteTextBox)base.Owner;
            if (owner.pop.IsOpen)
            {
                AutomationPeer item = UIElementAutomationPeer.CreatePeerForElement(owner.lstBox);
                if (item == null)
                    return childrenCore;

                childrenCore.Add(item);
            }
            return childrenCore;
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Value)
                return this;

            return base.GetPattern(patternInterface);
        }

        public bool IsReadOnly
        {
            get { return !((AutocompleteTextBox)base.Owner).IsEnabled; }
        }

        public void SetValue(string value)
        {
            AutocompleteTextBox ac = ((AutocompleteTextBox)base.Owner);
            ac.Text = value;
            ac.delayTimer_Tick(null, null);
            if (ac.AllowFreeText && ac.lstBox.Items.Count == 0)
                ac.Close(CloseReason.LostFocus);
        }

        public string Value
        {
            get { return ((AutocompleteTextBox)base.Owner).Text; }
        }
    }

    public enum CloseReason
    {
        ClickList,
        Enter,
        Tab,
        TabExit,
        Esc,
        LostFocus,
        ClickOut
    }

    public class CloseEventArgs : RoutedEventArgs
    {
        public CloseReason Reason { get; private set; }
        public bool IsCommit
        {
            get
            {
                return
                    Reason == CloseReason.Enter ||
                    Reason == CloseReason.Tab ||
                    Reason == CloseReason.ClickList;
            }
        }

        public CloseEventArgs(CloseReason reason)
            : base(AutocompleteTextBox.ClosedEvent)
        {
            Reason = reason;
        }
    }

    public delegate void ClosedEventHandler(object sender, CloseEventArgs e);

}
